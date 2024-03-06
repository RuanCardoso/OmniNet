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

using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable

namespace Omni.Core.Web
{
	public class NetworkHttpRequest
	{
		private readonly HttpListenerRequest _request;
		private readonly Stream _stream;

		public string UserAgent { get => _request.UserAgent; }
		public IPEndPoint RemoteEndPoint { get => _request.RemoteEndPoint; }
		public IPEndPoint LocalEndPoint { get => _request.LocalEndPoint; }
		public NameValueCollection Headers { get => _request.Headers; }
		public CookieCollection Cookies { get => _request.Cookies; }
		public Encoding ContentEncoding { get => _request.ContentEncoding; }
		public string ContentType { get => _request.ContentType; }
		public long ContentLength64 { get => _request.ContentLength64; }
		public bool IsLocal { get => _request.IsLocal; }

		internal NetworkHttpRequest(HttpListenerRequest httpListenerRequest)
		{
			_request = httpListenerRequest;
			_stream = httpListenerRequest.InputStream;
		}

		public string PostAsJson()
		{
			OmniLogger.PrintError(_request.HasEntityBody);
			using (StreamReader reader = new StreamReader(_request.InputStream, _request.ContentEncoding, true, (int)_request.ContentLength64, true))
			{
				return reader.ReadToEnd();
			}
		}

		public Task<string> PostAsync()
		{
			using (StreamReader reader = new StreamReader(_request.InputStream, _request.ContentEncoding, true, (int)_request.ContentLength64, true))
			{
				return reader.ReadToEndAsync();
			}
		}

		public Row PostAsRow()
		{
			return JsonConvert.DeserializeObject<Row>(PostAsJson());
		}

		public string Get(string name)
		{
			return _request.QueryString[name];
		}

		public T Get<T>(string name)
		{
			return (T)Convert.ChangeType(_request.QueryString[name], typeof(T));
		}

		public bool TryGet(string name, out string value)
		{
			try
			{
				value = _request.QueryString[name];
				return !string.IsNullOrEmpty(value);
			}
			catch
			{
				value = default;
				return false;
			}
		}

		public bool TryGet<T>(string name, out T value)
		{
			try
			{
				string parameter = _request.QueryString[name];
				if (!string.IsNullOrEmpty(parameter))
				{
					value = (T)Convert.ChangeType(parameter, typeof(T));
					return true;
				}
				else
				{
					value = default;
					return false;
				}
			}
			catch
			{
				value = default;
				return false;
			}
		}
	}
}