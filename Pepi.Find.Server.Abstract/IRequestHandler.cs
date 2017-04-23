#region using
using System.Threading.Tasks;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface IRequestHandler
	{
		Task ProcessRequestAsync(IHandlerContext context);
	}
}
