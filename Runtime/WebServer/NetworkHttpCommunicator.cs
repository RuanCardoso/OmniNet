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
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;

#pragma warning disable

namespace Omni.Core.Web
{
	public class NetworkHttpCommunicator
	{
		private string _url;
		private HttpListener HttpListener { get; } = new HttpListener();
		private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
		private ConcurrentDictionary<string, Action<NetworkHttpResponse, NetworkHttpRequest>> Routes { get; } = new();

		public async void Initialize()
		{
			if (!HttpListener.IsSupported)
			{
				OmniLogger.PrintError("HTTP listener is not supported on this platform.");
			}

			_url = "http://*:8080/";
			HttpListener.Prefixes.Add($"{_url}");
			HttpListener.Start();
			while (!CancellationTokenSource.IsCancellationRequested)
			{
				try
				{
					HttpListenerContext context = await HttpListener.GetContextAsync();
					HttpListenerRequest request = context.Request;
					using (HttpListenerResponse response = context.Response)
					{
						using (Stream inputStream = request.InputStream)
						{
							using (Stream outputStream = response.OutputStream)
							{
								string path = request.Url.AbsolutePath;
								string route = Path.HasExtension(path) ? $"/{Path.GetFileNameWithoutExtension(path)}" : path;
								if (request.HttpMethod == "GET" || request.HttpMethod == "POST")
								{
									if (Routes.TryGetValue(route, out Action<NetworkHttpResponse, NetworkHttpRequest> exec))
									{
										NetworkHttpRequest httpRequest = new NetworkHttpRequest(request);
										NetworkHttpResponse httpResponse = new NetworkHttpResponse(response);
										exec(httpResponse, httpRequest);
									}
								}
								response.Headers.Clear();
							}
						}
					}
				}
				catch (ObjectDisposedException) { }
				catch (Exception ex)
				{
					OmniLogger.PrintError($"Http Server: {ex.Message}");
				}
			}
		}

		internal void AddRoute(string route, Action<NetworkHttpResponse, NetworkHttpRequest> res)
		{
			if (!Routes.TryAdd(route, res))
			{
				OmniLogger.PrintError("Failed to add route: " + route);
			}
		}

		public void Close()
		{
			try
			{
				CancellationTokenSource.Cancel();
				HttpListener.Stop();
			}
			finally
			{
				CancellationTokenSource.Dispose();
				HttpListener.Close();
			}
		}
	}
}