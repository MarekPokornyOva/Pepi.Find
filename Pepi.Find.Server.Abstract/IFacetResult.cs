#region using
using System;
using System.Collections.Generic;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface IFacetResult:IDisposable
	{
		int Missing { get; }
		int Total { get; }
		int Other { get; }
		IEnumerable<FacetResultTermItem> Terms { get; }
	}
}
