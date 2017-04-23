#region using
using Microsoft.AspNetCore.Http;
using Pepi.Find.Server.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#endregion using

namespace Pepi.Find.WebService.Util
{
	public class WebContext:IHandlerContext
	{
		public IHandlerRequest Request => _request;
		public IHandlerResponse Response => _response;

		IHandlerRequest _request;
		IHandlerResponse _response;
		public WebContext(HttpContext context)
		{
			_request=new WebRequest(context.Request);
			_response=new WebResponse(context.Response);
		}
	}

	class WebRequest:IHandlerRequest
	{
		HttpRequest _request;
		internal WebRequest(HttpRequest request)
		{
			_request=request;
			_headers=new LateProperty<IDictionary<string,string[]>>(() => _request.Headers.ToDictionary(x => x.Key,x => x.Value.ToArray()));
		}

		readonly LateProperty<IDictionary<string,string[]>> _headers;

		public string RawUrl => _request.Path;
		public string UserAgent => _request.Headers["User-Agent"];
		public IDictionary<string,string[]> Headers => _headers.Value;
		public Stream Body => _request.Body;
	}

	class WebResponse:IHandlerResponse
	{
		private HttpResponse _response;
		public WebResponse(HttpResponse response) => _response=response;

		public Stream Body => _response.Body;
		public int StatusCode { get => _response.StatusCode; set => _response.StatusCode=value; }
	}

	#region LateProperty
	class LateProperty<T>
	{
		Func<T> _getter;
		internal LateProperty(Func<T> getter)
		{
			_getter=getter;
		}

		bool _loaded;
		T _value;
		internal T Value
		{
			get
			{
				if (!_loaded)
				{
					_loaded=true;
					_value=_getter();
				}
				return _value;
			}
		}
	}
	#endregion LateProperty
}
