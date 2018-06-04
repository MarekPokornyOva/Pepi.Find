#region using
using System.Collections.Generic;
using System.Linq;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public class SearchResultItem
	{
		public virtual SearchResultIndex Index { get; } = new SearchResultIndex();
		public virtual SearchResultDocument Document { get; } = new SearchResultDocument();
		public virtual SearchResultHighlight Highlights { get; } = new SearchResultHighlight();
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
		public virtual int Id { get; set; }
		public virtual string LanguageName { get; set; }
		public virtual IEnumerable<string> Types { get; set; }
		public virtual IEnumerable<KeyValuePair<string,object>> FieldValues { get; set; }
	}

	public class SearchResultHighlight
	{
		public IEnumerable<SearchFieldHighlights> Fields { get; set; } = Enumerable.Empty<SearchFieldHighlights>();
	}

	public class SearchFieldHighlights
	{
		public string FieldName { get; set; }

		public IEnumerable<string> Highlights { get; set; }
	}
}
