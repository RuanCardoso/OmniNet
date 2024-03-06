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

using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Omni.Core.Web
{
	public class NetworkHttpClient
	{
		public class HttpClient
		{
			public async Task<HttpResponseMessage> PostAsync(string uri, HttpContent httpContent, CancellationToken cancellationToken = default)
			{
				if (!ReuseHttpClient)
				{
					using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
					{
						return await httpClient.PostAsync(uri, httpContent, cancellationToken);
					}
				}
				else
				{
					return await InternalHttpClient.PostAsync(uri, httpContent, cancellationToken);
				}
			}

			public Task<HttpResponseMessage> PostAsync(string uri, string jsonObject, Encoding encoding = null, CancellationToken cancellationToken = default)
			{
				encoding ??= Encoding.UTF8;
				return PostAsync(uri, new StringContent(jsonObject, encoding, "application/json"), cancellationToken);
			}

			public Task<HttpResponseMessage> PostAsync(string uri, object @object, Encoding encoding = null, CancellationToken cancellationToken = default)
			{
				encoding ??= Encoding.UTF8;
				return PostAsync(uri, JsonConvert.SerializeObject(@object), encoding, cancellationToken);
			}

			public async Task<HttpResponseMessage> GetAsync(string uri, HttpCompletionOption httpCompletionOption = default, CancellationToken cancellationToken = default)
			{
				if (!ReuseHttpClient)
				{
					using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
					{
						return await httpClient.GetAsync(uri, httpCompletionOption, cancellationToken);
					}
				}
				else
				{
					return await InternalHttpClient.GetAsync(uri, httpCompletionOption, cancellationToken);
				}
			}
		}

		private static System.Net.Http.HttpClient InternalHttpClient { get; } = new System.Net.Http.HttpClient();
		public static bool ReuseHttpClient { get; set; }
		public static HttpClient Client { get; } = new HttpClient();
	}
}