#if UNITY_EDITOR
using Newtonsoft.Json;
using Omni.Core;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[JsonObject(MemberSerialization.OptIn)]
public class SshWindow : EditorWindow
{
	// Define o tamanho original da Janela, porque depois alteramos e precisamos e voltar pro tamanho anterior.
	private readonly float xOriginalSize = 200;
	private readonly float yOriginalSize = 203;

	// Tamanho atual das janelas!
	private float xSize;
	private float ySize;

	private Vector2 currentScroll = Vector3.zero;
	private Rect dockPosition = Rect.zero;

	private SshClient sshClient;
	private SftpClient sftpClient;
	private ShellStream shellStream;

	private bool isDirectory;
	private string pathToUpload;
	private string commandOutput = "";
	private string commandOutputToCompareAndScroll = "";
	private StringBuilder commandOutputBuilder;

	[JsonProperty]
	private int port = 22; // Porta ssh, não esqueça de liberar no firewall;
	[JsonProperty]
	private string host;
	[JsonProperty]
	private string username;
	[JsonProperty]
	private string keyPath;

	[JsonProperty]
	private bool usePersistentSession = false;
	[JsonProperty]
	private bool hasSession;
	private bool isConnected; // Quando o ssh está conectado e funcionando.
	private bool moveCursorToEnd;

	[JsonProperty]
	private readonly List<string> commands = new List<string>();
	[JsonProperty]
	private int commandIndex = 0;

	[MenuItem("Omni/SSH Client")]
	public static void ShowExample()
	{
		SshWindow wnd = GetWindow<SshWindow>();
		wnd.minSize = new Vector2(wnd.xOriginalSize, wnd.yOriginalSize);
		wnd.maxSize = new Vector2(wnd.xOriginalSize, wnd.yOriginalSize);
		wnd.titleContent = new GUIContent("SSH Client for Omni");
		wnd.dockPosition = wnd.position;
	}

	private void OnDisable()
	{
		shellStream?.Close();
		shellStream?.Dispose();
		sftpClient?.Dispose();
		sshClient?.Dispose();
	}

	private void OnEnable()
	{
		GetConnectionInfo();
		if (isConnected)
		{
			Connect();
		}
	}

	private void OnDestroy()
	{
		isConnected = false;
	}

	private void OnInspectorUpdate()
	{
		// Força a janela a atualizar pras informações mas recentes.
		Repaint();
	}

	private void OnGUI()
	{
		ListenInput();
		GUILayout.BeginVertical();
		if (!isConnected)
		{
			// Vamos restaurar ao tamanho original da janela em caso de desconexão....
			xSize = xOriginalSize;
			ySize = yOriginalSize;

			minSize = new Vector2(xSize, ySize);
			maxSize = new Vector2(xSize, ySize);
			// Vamos undock esta janela, o terminal é permitido "dock", mas a janela de conexão não..
			if (docked)
			{
				if (Event.current.type == EventType.Repaint)
				{
					position = dockPosition;
				}
			}

			// Vamos desenhar a janela de conexão!
			GUILayout.Label("Host:");
			host = GUILayout.TextField(host);
			GUILayout.Label("Port:");
			port = EditorGUILayout.IntField(int.Parse(port.ToString()));
			GUILayout.Label("Username:");
			username = GUILayout.TextField(username);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Private Key:"); ;
			if (GUILayout.Button("Find Private Key", GUILayout.ExpandWidth(false)))
			{
				keyPath = EditorUtility.OpenFilePanel("Private Key", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pem, ppk, key");
			}

			GUILayout.EndHorizontal();
			keyPath = GUILayout.TextField(keyPath);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Use Persistent Session?");
			usePersistentSession = EditorGUILayout.Toggle(usePersistentSession, GUILayout.Width(xSize * xSize));
			GUILayout.EndHorizontal();
			// Start Connection
			if (GUILayout.Button("Connect", GUILayout.ExpandWidth(true)))
			{
				if (keyPath == null || string.IsNullOrEmpty(keyPath))
				{
					OmniLogger.PrintError("Select the private key.");
				}
				else
				{
					SaveConnectionInfo();
					Connect();
				}
			}
		}
		else
		{
			// Vamos restaurar ao tamanho original do terminal após a conexão...
			xSize = 950;
			ySize = 550;

			minSize = new Vector2(xSize, ySize);
			maxSize = new Vector2(xSize, ySize);
			// Vamos desenhar o terminal!
			GUILayout.Space(3);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Select a folder", GUILayout.ExpandWidth(false)))
			{
				isDirectory = true;
				pathToUpload = EditorUtility.OpenFolderPanel("Select a folder", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Any folder");
			}

			if (GUILayout.Button("Select a file", GUILayout.ExpandWidth(false)))
			{
				isDirectory = false;
				pathToUpload = EditorUtility.OpenFilePanel("Select a file", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "");
			}

			if (GUILayout.Button("Upload", GUILayout.ExpandWidth(false)))
			{
				if (!isDirectory)
				{
					_ = UploadFile(pathToUpload, Path.GetFileName(pathToUpload));
				}
				else
				{
					UploadDirectory(pathToUpload);
				}
			}

			GUILayout.EndHorizontal();
			GUILayout.Space(3);
			pathToUpload = GUILayout.TextField(pathToUpload, GUILayout.ExpandWidth(true));
			GUILayout.Space(3);
			currentScroll = EditorGUILayout.BeginScrollView(currentScroll);
			commandOutput = GUILayout.TextArea(commandOutput, new GUIStyle(GUI.skin.textArea)
			{
				fontSize = 13,
				fontStyle = FontStyle.BoldAndItalic,
				richText = true
			}, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndScrollView();
			TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			if (textEditor != null)
			{
				// Não permite a edição da saída do buffer, mas permite a cópia.
				if (commandOutput.Length <= commandOutputBuilder.Length)
				{
					textEditor.selectIndex = commandOutput.Length;
					commandOutput = commandOutputBuilder.ToString();
				}
				else
				{
					if (textEditor.selectIndex <= commandOutputBuilder.Length)
					{
						textEditor.selectIndex = commandOutputBuilder.Length;
					}
				}

				var e = Event.current;
				if (e.type == EventType.Repaint)
				{
					// Vai mover o scroll para o final sempre que a saída mudar para maior!
					if (commandOutput != commandOutputToCompareAndScroll)
					{
						int lenDiff = commandOutput.Length - commandOutputToCompareAndScroll.Length;
						if (lenDiff > 0)
						{
							currentScroll.y = float.MaxValue;
						}
						commandOutputToCompareAndScroll = commandOutput;
					}
				}

				// Vai mover o cursor para o final sempre que receber dados!
				if (moveCursorToEnd)
				{
					textEditor.selectIndex = commandOutput.Length;
					textEditor.cursorIndex = commandOutput.Length;
					textEditor.altCursorPosition = commandOutput.Length;
					moveCursorToEnd = false;
				}
			}
			GUILayout.Space(10);
		}
		GUILayout.EndVertical();
	}

	private void ListenInput()
	{
		var current = Event.current;
		if (isConnected && (current.type == EventType.KeyDown || current.type == EventType.MouseDown))
		{
			switch (current.keyCode)
			{
				case KeyCode.Escape:
					SendDontWait("\u0003");
					break;
				case KeyCode.UpArrow:
					commandIndex++;
					SelectCommandWithArrowKey(commandIndex);
					break;
				case KeyCode.DownArrow:
					commandIndex--;
					SelectCommandWithArrowKey(commandIndex);
					break;
				case KeyCode.Return:
					{
						string command = commandOutput[commandOutputBuilder.Length..];
						if (command == null && command.Length <= 0)
						{
							SendDontWait("\n");
						}
						else
						{
							if (command.ToLowerInvariant() == "clear")
							{
								commandOutput = "";
								commandOutputBuilder.Clear();
								commandOutputToCompareAndScroll = "";
								SendDontWait(command);
							}
							else
							{
								SendDontWait(command);
							}
						}
					}
					break;
			}
		}
	}

	private void SelectCommandWithArrowKey(int index)
	{
		if (commands.Count > 0)
		{
			commandOutput = commandOutputBuilder.ToString();
			index %= commands.Count - 1;
			commandOutput += commands[index];
			moveCursorToEnd = true;
		}
	}

	private void Connect()
	{
		try
		{
			// SSH
			commandOutputBuilder = new StringBuilder();
			sshClient ??= new SshClient(new ConnectionInfo(host, port, username, new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(keyPath))));
			sshClient.Connect();
			// SFTP
			sftpClient ??= new SftpClient(sshClient.ConnectionInfo);
			sftpClient.Connect();
			// SSH Stream
			shellStream ??= sshClient.CreateShellStream("Omni Terminal", 80, 24, 800, 600, 1024);
			shellStream.DataReceived += ShellStream_DataReceived;
			isConnected = true;
			OmniLogger.Print("SSH connection established.");
			// Vamos configurar algumas coisas antes de começar!
			Setup();
		}
		catch (Exception ex)
		{
			OmniLogger.LogStacktrace(ex);
			if (ex.InnerException != null)
			{
				OmniLogger.LogStacktrace(ex.InnerException);
			}
		}
	}

	private void Setup()
	{
		SendDontWait("export TERM=xterm", true);
		if (usePersistentSession)
		{
			if (!hasSession)
			{
				hasSession = true;
				SendDontWait("sudo screen -S omni", true);
				SaveConnectionInfo();
			}
			else
			{
				SendDontWait("sudo screen -r omni", true);
			}
		}
	}

	private void AddOutput(string output)
	{
		commandOutputBuilder.Append(Regex.Replace(output, @"\x1B[@-_][0-?]*[ -/]*[@-~]", ""));
		commandOutput = commandOutputBuilder.ToString();
		moveCursorToEnd = true;
	}

	private void ShellStream_DataReceived(object sender, Renci.SshNet.Common.ShellDataEventArgs e)
	{
		string output = Encoding.UTF8.GetString(e.Data);
		if (output != null && output.Length > 0)
		{
			AddOutput(output);
		}
	}

	// Escreve sem bloquear, para mantermos o fluxo em tempo real.
	private void SendDontWait(string command, bool internalCommand = false)
	{
		if (command != null && command.Length > 0)
		{
			if (!internalCommand)
			{
				commands.Add(command);
				SaveConnectionInfo();
			}
			shellStream.WriteLine(command);
		}
	}

	// Bloqueia, não é possível obter dados em tempo real.
	private Task SendWaitAsync(string command, bool internalCommand = false)
	{
		return Task.Run(() =>
		{
			if (command != null && command.Length > 0)
			{
				if (!internalCommand)
				{
					commands.Add(command);
					SaveConnectionInfo();
				}

				using (SshCommand sshCommand = sshClient.RunCommand(command))
				{
					string result = sshCommand.Result;
					if (result != null && result.Length > 0)
					{
						AddOutput(result);
					}

					string error = sshCommand.Error;
					if (error != null && error.Length > 0)
					{
						AddOutput(error);
					}

					using (StreamReader outputStream = new StreamReader(sshCommand.OutputStream))
					{
						string outStream = outputStream.ReadToEnd();
						if (outStream != null && outStream.Length > 0)
						{
							AddOutput(outStream);
						}

						using (StreamReader extendedOutputStream = new StreamReader(sshCommand.ExtendedOutputStream))
						{
							string extendedOutStream = extendedOutputStream.ReadToEnd();
							if (extendedOutStream != null && extendedOutStream.Length > 0)
							{
								AddOutput(extendedOutStream);
							}
						}
					}
				}
			}
		});
	}

	private async void UploadDirectory(string baseDir)
	{
		// Vamos upar a pasta pro servidor.
		commandOutput += "Creating folder hierarchy, please wait!";
		await Task.Run(() =>
		{
			var dirs = Directory.GetDirectories(baseDir, "*", SearchOption.AllDirectories).OrderBy(x => x.Length);
			string lastFolderName = Path.GetFileName(Path.GetDirectoryName(baseDir + "/"));
			string rootFolder = Path.Combine(lastFolderName, Path.GetRelativePath(baseDir, dirs.First())).Split(@"\")[0];
			// Antes vamos criar a pasta raiz que contem nosso conteúdo.
			// Pasta0/Pasta1/Pasta2/arquivo.txt
			// Pasta0 é a raiz e vai ser criada se ela não existir.
			if (!sftpClient.Exists(rootFolder))
			{
				sftpClient.CreateDirectory(rootFolder);
				MkDir(baseDir, dirs, lastFolderName);
			}
			else
			{
				MkDir(baseDir, dirs, lastFolderName);
			}

			void MkDir(string baseDir, IOrderedEnumerable<string> dirs, string lastFolderName)
			{
				foreach (string dir in dirs)
				{
					// Antes de criar as pastas replace \ to / porque linux não suporta \, mas windows suporta os dois!
					string pathName = Path.Combine(lastFolderName, Path.GetRelativePath(baseDir, dir)).Replace(@"\", "/");
					if (!sftpClient.Exists(pathName))
					{
						sftpClient.CreateDirectory(pathName);
					}
				}
			}
		});

		// Após a criação da hierarquia de pastas podemos enviar os arquivos.
		var paths = Directory.GetFiles(baseDir, "*.*", SearchOption.AllDirectories);
		string lastFolderName = Path.GetFileName(Path.GetDirectoryName(baseDir + "/"));
		foreach (string filePath in paths)
		{
			if (!filePath.Contains("ButDontShipItWithYourGame") && !filePath.Contains("DoNotShip"))
			{
				string pathName = Path.Combine(lastFolderName, Path.GetRelativePath(baseDir, filePath)).Replace(@"\", "/");
				await UploadFile(filePath, pathName);
			}
		}
	}

	private async Task UploadFile(string path, string fileName)
	{
		double percentage = await Task.Run(() =>
		{
			using (var fileStream = new FileStream(path, FileMode.Open))
			{
				try
				{
					double percentage = 0;
					string workingDir = sftpClient.WorkingDirectory + "/" + fileName;
					// Upload de apenas um arquivo, mas simples.
					sftpClient.UploadFile(fileStream, workingDir, (progress) =>
					{
						percentage = progress / (double)fileStream.Length * 100.0d;
						commandOutput = $"{commandOutputBuilder} Upload: {progress} bytes / {fileStream.Length} bytes ({Math.Round(percentage, 2)}%)";
					});
					return percentage;
				}
				catch (Exception)
				{
					return 0;
				}
			}
		});

		if (percentage == 100)
		{
			commandOutputBuilder.AppendLine($"<color=#0ced1b>Upload successful: {fileName}</color>");
		}
		else
		{
			commandOutputBuilder.AppendLine($"<color=#ff0f0f>Error uploading file: {fileName}</color>");
			commandOutputBuilder.AppendLine($"<color=#e2ed0e>Trying to upload the file again: {fileName}</color>");
			await UploadFile(path, fileName);
		}

		commandOutput = commandOutputBuilder.ToString();
		moveCursorToEnd = true;
	}

	private void SaveConnectionInfo()
	{
		using (StreamWriter writer = new StreamWriter("settings.ini", false))
		{
			writer.Write(JsonConvert.SerializeObject(this));
		}
	}

	private void GetConnectionInfo()
	{
		if (File.Exists("settings.ini"))
		{
			JsonConvert.PopulateObject(File.ReadAllText("settings.ini"), this);
		}
	}
}
#endif