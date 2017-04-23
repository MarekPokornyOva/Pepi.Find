#region using
using System.Collections.Generic;
using System.IO;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface IHandlerRequest
	{
		string RawUrl { get; }
		string UserAgent { get; }
		IDictionary<string,string[]> Headers { get; }
		Stream Body { get; }
	}
}
