#region using
using System.Collections.Generic;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface IDeleteByQueryResult
	{
		IEnumerable<DeleteResultItem> DeleteResults { get; }
	}
}
