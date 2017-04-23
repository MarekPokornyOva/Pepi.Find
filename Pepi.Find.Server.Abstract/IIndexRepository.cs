#region using
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface IIndexRepository
	{
		Task SaveIndexItemsAsync(List<IndexItem> indexItems);
		ISearchQueryBuilder CreateSearchQueryBuilder();
		IFacetQueryBuilder CreateFacetQueryBuilder();
	}
}
