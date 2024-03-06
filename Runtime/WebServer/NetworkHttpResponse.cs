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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Omni.Core.Web
{
	public class NetworkHttpResponse
	{
		private readonly HttpListenerResponse _response;
		private readonly Stream _stream;

		public Encoding ContentEncoding { get => _response.ContentEncoding; set => _response.ContentEncoding = value; }
		public WebHeaderCollection Headers { get => _response.Headers; set => _response.Headers = value; }
		public CookieCollection Cookies { get => _response.Cookies; set => _response.Cookies = value; }
		public string ContentType { get => _response.ContentType; set => _response.ContentType = value; }
		public long ContentLength64 { get => _response.ContentLength64; private set => _response.ContentLength64 = value; }
		public int StatusCode { get => _response.StatusCode; set => _response.StatusCode = value; }
		public string StatusDescription { get => _response.StatusDescription; set => _response.StatusDescription = value; }

		internal NetworkHttpResponse(HttpListenerResponse httpListenerResponse)
		{
			_response = httpListenerResponse;
			_response.ContentType = "text/html; charset=utf-8";
			_response.ContentEncoding = Encoding.UTF8;
			_response.StatusCode = (int)HttpStatusCode.OK;
			_stream = _response.OutputStream;
		}

		public void Send(string res)
		{
			byte[] sPtr = ContentEncoding.GetBytes(res);
			Send(sPtr, 0, sPtr.Length);
		}

		public void Send(ReadOnlySpan<byte> buffer)
		{
			ContentLength64 = buffer.Length;
			_stream.Write(buffer);
		}

		public void Send(byte[] buffer, int offset, int count)
		{
			ContentLength64 = count - offset;
			_stream.Write(buffer, offset, count);
		}

		public ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			ContentLength64 = buffer.Length;
			return _stream.WriteAsync(buffer, cancellationToken);
		}

		public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
		{
			ContentLength64 = count - offset;
			return _stream.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public void AddHeader(string name, string value)
		{
			_response.AddHeader(name, value);
		}

		public void AppendCookie(Cookie cookie)
		{
			_response.AppendCookie(cookie);
		}

		public void AppendHeader(string name, string value)
		{
			_response.AppendHeader(name, value);
		}

		public void SetCookie(Cookie cookie)
		{
			_response.SetCookie(cookie);
		}
	}
}