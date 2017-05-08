#region using
using System.Collections.Generic;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public class IndexItem
	{
		public IndexItemObject Object { get; } = new IndexItemObject();
		public IndexItemContent Content { get; } = new IndexItemContent();
	}

	public class IndexItemObject
	{
		public string Id { get; set; }
		public string Type { get; set; }
		public string IndexName { get; set; }
		public int Version { get; set; }
	}

	public class IndexItemContent
	{
		public List<KeyValuePair<string,object>> Values { get; } = new List<KeyValuePair<string,object>>();
	}
}
