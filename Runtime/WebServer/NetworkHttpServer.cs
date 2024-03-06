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

#pragma warning disable

using System;

namespace Omni.Core.Web
{
	public static class NetworkHttpServer
	{
		public class HttpServer
		{
			public void Post(string route, Action<NetworkHttpResponse, NetworkHttpRequest> res)
			{
				WebServer.AddRoute(route, res);
			}

			public void Get(string route, Action<NetworkHttpResponse, NetworkHttpRequest> res)
			{
				WebServer.AddRoute(route, res);
			}
		}

		public static HttpServer Server { get; } = new HttpServer();
		public static NetworkHttpCommunicator WebServer { get; set; }
	}
}