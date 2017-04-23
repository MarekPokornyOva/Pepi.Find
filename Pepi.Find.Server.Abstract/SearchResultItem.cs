#region using
using System.Collections.Generic;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public class SearchResultItem
	{
		public virtual SearchResultIndex Index { get; } = new SearchResultIndex();
		public virtual SearchResultDocument Document { get; } = new SearchResultDocument();
	}

	public class SearchResultIndex
	{
		public virtual string Name { get; set; }
		public virtual string ContentTypeName { get; set; }
		public virtual string Id { get; set; }
		public virtual int? Score { get; set; }
	}

	public class SearchResultDocument
	{
		public virtual long Id { get; set; }
		public virtual string LanguageName { get; set; }
		public virtual List<string> Types { get; } = new List<string>();
	}
}
