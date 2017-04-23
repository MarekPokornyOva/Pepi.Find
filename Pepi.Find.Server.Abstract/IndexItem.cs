#region using
using System.Collections.Generic;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public class IndexItem
	{
		public IndexItemIndex Index { get; } = new IndexItemIndex();
		public IndexItemContent Content { get; } = new IndexItemContent();
	}

	public class IndexItemIndex
	{
		public string Id { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }
	}

	public class IndexItemContent
	{
		public List<KeyValuePair<string,object>> Values { get; } = new List<KeyValuePair<string,object>>();
	}
}
