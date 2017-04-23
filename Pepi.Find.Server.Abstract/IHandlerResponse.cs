#region using
using System.IO;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface IHandlerResponse
	{
		Stream Body { get; }
		int StatusCode { get; set; }
	}
}
