#region using
using System.Threading.Tasks;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface ISearchQueryBuilder:IQueryBuilder
	{
		void SetSkipSize(long? skipSize);
		void SetResultSize(long? resultSize);
		void AddSort(ISortInfo[] sort);
		Task<ISearchResult> ExecuteAsync();
	}
}
