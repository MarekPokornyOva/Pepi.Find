#region using
using System.Threading.Tasks;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface IFacetQueryBuilder
	{
		void SetField(string fieldName);
		void SetResultSize(int? resultSize);
		Task<IFacetResult> ExecuteAsync();
	}
}
