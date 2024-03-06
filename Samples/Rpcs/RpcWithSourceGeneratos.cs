using Newtonsoft.Json;
using Omni.Core;
using UnityEngine;
using static Omni.Core.Web.NetworkHttpClient;
using static Omni.Core.Web.NetworkHttpServer;

namespace Omni.Internal.Samples
{
	public partial class RpcWithSourceGeneratos : MonoBehaviour
	{
		private void Awake()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 60;
		}

		private void Start()
		{
			//ReuseHttpClient = true;
			Server.Post("/ruan", (res, req) =>
			{
				res.Send($"Resposta do servidor! {req.PostAsJson()}");
			});
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.V))
			{
				Test();
			}

			if (Input.GetKeyDown(KeyCode.L))
			{
				Test2();
			}
		}

		private async void Test()
		{
			string json = JsonConvert.SerializeObject(new
			{
				Name = "Ruan",
				Id = 2,
			});

			using (var response = await Client.PostAsync("http://127.0.0.1:8080/ruan", json))
			{
				response.EnsureSuccessStatusCode();
				if (response.IsSuccessStatusCode)
				{
					OmniLogger.PrintError(await response.Content.ReadAsStringAsync());
				}
			}
		}


		private async void Test2()
		{
			for (int i = 0; i < 500; i++)
			{
				string json = JsonConvert.SerializeObject(new
				{
					Name = "Ruan",
					Id = 2,
				});

				using (var response = await Client.PostAsync("http://127.0.0.1:8080/ruan", json))
				{
					OmniLogger.PrintError(await response.Content.ReadAsStringAsync());
				}
			}
		}
	}
}