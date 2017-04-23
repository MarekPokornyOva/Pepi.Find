#region using
using System;
using System.Collections.Generic;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface ISearchResult:IDisposable
	{
		IEnumerable<SearchResultItem> Items { get; }
		int Count { get; }
	}
}
