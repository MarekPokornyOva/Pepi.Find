#region using
using System.Threading.Tasks;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface IDeleteByQueryBuilder:IQueryBuilder
	{
		Task<IDeleteByQueryResult> ExecuteAsync();
	}
}
