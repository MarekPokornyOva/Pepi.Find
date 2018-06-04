#region using
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion using

namespace Pepi.Find.Server.Abstract
{
	public interface ISearchQueryBuilder:IQueryBuilder
	{
		void SetRequestedFields(IEnumerable<string> fields);
		void SetScriptFields(IEnumerable<ScriptField> fields);
		void SetSkipSize(long? skipSize);
		void SetResultSize(long? resultSize);
		void AddSort(ISortInfo[] sort);
		void SetHighlights(string query,IEnumerable<HighlightFieldRequest> fields);
		Task<ISearchResult> ExecuteAsync();
	}

	public class ScriptField
	{
		public ScriptField(string name,string script,string language,Dictionary<string,object> parameters)
		{
			Name=name;
			Script=script;
			Language=language;
			Parameters=parameters;
		}

		public string Name { get; }
		public string Script { get; }
		public string Language { get; }
		public Dictionary<string,object> Parameters { get; }
	}

	public class HighlightFieldRequest
	{
		public HighlightFieldRequest(string fieldName,IEnumerable<string> preTags,IEnumerable<string> postTags)
		{
			FieldName=fieldName;
			PreTags=preTags;
			PostTags=postTags;
		}

		public string FieldName { get; }
		public IEnumerable<string> PreTags { get; }
		public IEnumerable<string> PostTags { get; }
	}
}