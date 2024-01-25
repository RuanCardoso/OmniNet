/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System;
using UnityEngine;

namespace Omni.Core
{
	/// <summary>
	/// This class is used to print messages in the console.
	/// Print methods are used to print messages in the console without stacktrace.
	/// Log methods are used to print messages in the console with stacktrace.
	/// </summary>
	public static class OmniLogger
	{
#if !UNITY_SERVER || UNITY_EDITOR
		public static void Print(object message) => Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
		public static void PrintError(object message) => Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "{0}", message);
		public static void PrintWarning(object message) => Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "{0}", message);
		public static void Log(object message) => Debug.LogFormat(LogType.Log, LogOption.None, null, "{0}", message);
		public static void LogError(object message) => Debug.LogFormat(LogType.Error, LogOption.None, null, "{0}", message);
		public static void LogWarning(object message) => Debug.LogFormat(LogType.Warning, LogOption.None, null, "{0}", message);
		public static void LogStacktrace(Exception message) => Debug.LogException(message);
		public static void Inline(object message) => Print(string.Format("\r{0}", message));
		public static void Clear() => Debug.ClearDeveloperConsole();
#else
		public static void Print(object message) => Print(message, "INFO");
		public static void PrintError(object message) => Print(message, "ERROR");
		public static void PrintWarning(object message) => Print(message, "WARNING");
		public static void Log(object message) => Print(message, "INFO");
		public static void LogError(object message) => Print(message, "ERROR");
		public static void LogWarning(object message) => Print(message, "WARNING");
		public static void LogStacktrace(Exception message) => Print(message, "ERROR");
		public static void Inline(object message) => Print(string.Format("\r{0}", message), "INFO");
		public static void Clear() => Console.Clear();

		public static void Print(object message, string prefix)
		{
			ConsoleColor color = prefix switch
			{
				"ERROR" => ConsoleColor.Red,
				"WARNING" => ConsoleColor.Yellow,
				_ => ConsoleColor.White,
			};

			Console.ForegroundColor = color;
			Console.WriteLine($"[{prefix}] {message}");
			Console.WriteLine(new string('-', Console.WindowWidth - 1));
		}
#endif
	}
}