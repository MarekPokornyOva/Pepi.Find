#region using
using EPiServer.Find;
using EPiServer.Find.Api;
using EPiServer.Find.Api.Facets;
using EPiServer.Find.Api.Ids;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Api.Querying.Filters;
using EPiServer.Find.Api.Querying.Queries;
using EPiServer.Find.ClientConventions;
using EPiServer.Find.Connection;
using EPiServer.Find.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pepi.Find.Server.Abstract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#endregion using

namespace Pepi.Find.Direct
{
	public class DirectClient:IClient
	{
		IIndexRepository _indexRepository;
		string _defaultIndex;

		public DirectClient(IIndexRepository indexRepository)
		{
			_indexRepository=indexRepository;

			_defaultIndex=Configuration.GetConfiguration()?.DefaultIndex??"Index";
		}

		IClientConventions _conventions;
		public IClientConventions Conventions
		{
			get { return _conventions??(_conventions=new DefaultConventions(/*this*/)); }
			set { _conventions=value; }
		}

		Action<JsonSerializer> _customizeSerializer = new Action<JsonSerializer>(x => { });
		public Action<JsonSerializer> CustomizeSerializer => _customizeSerializer;

		public string DefaultIndex => _defaultIndex;

		public string ServiceUrl => throw new NotImplementedException();

		Settings _settings;
		static object _settingsLoadLock=new object();
		public Settings Settings
		{
			get
			{
				if (_settings==null)
					lock (_settingsLoadLock)
						if (_settings==null)
						{
							Languages langs;
							Settings settings=new Settings { Languages=langs=new Languages() };
							foreach (Language l in Language.GetAll())
								langs.Add(l.FieldSuffix);
							_settings=settings;
						}
				return _settings;
				
			}
		}

		public DeleteResult Delete(Type type,DocumentId id,Action<DeleteCommand> commandAction)
		{
			throw new NotImplementedException();
		}

		public DeleteResult Delete<T>(DocumentId id)
		{
			throw new NotImplementedException();
		}

		public DeleteResult Delete<T>(DocumentId id,Action<DeleteCommand> commandAction)
		{
			throw new NotImplementedException();
		}

		public DeleteByQueryResult DeleteByQuery(IQuery query,Action<DeleteByQueryCommand> commandAction)
		{
			DeleteByQueryCommandWrap dbqc = new DeleteByQueryCommandWrap();
			commandAction(dbqc);

			IDeleteByQueryBuilder dbqb = _indexRepository.CreateDeleteQueryBuilder();
			WriteFilter(((ConstantScoreQuery)query).Filter,dbqb);
			IDeleteByQueryResult res = dbqb.ExecuteAsync().Result;

			Indices indices = new Indices();
			foreach (DeleteResultItem item in res.DeleteResults)
			{
				int count = item.DeletedCount;
				indices.Add(new Index { Name=item.IndexName,Shards=new Shards { Total=count,Successful=count,Failed=0 } });
			}
			return new DeleteByQueryResult { Ok=true,Indices=indices };
		}

		public IEnumerable<GetResult<TSource>> Get<TSource>(IEnumerable<DocumentId> ids)
		{
			throw new NotImplementedException();
		}

		public TSource Get<TSource>(DocumentId id)
		{
			throw new NotImplementedException();
		}

		public TSource Get<TSource>(DocumentId id,Action<GetCommand<TSource>> commandAction)
		{
			throw new NotImplementedException();
		}

		public string GetServiceUrlWithDefaultIndex()
		{
			//throw new NotImplementedException();
			return "/";
		}

		public SettingsResult GetSettings()
		{
			throw new NotImplementedException();
		}

		public GetResult<TSource> GetWithMeta<TSource>(DocumentId id,Action<GetCommand<TSource>> commandAction)
		{
			throw new NotImplementedException();
		}

		public BulkResult Index(IEnumerable objectsToIndex)
		{
			List<IndexItem> items=objectsToIndex.Cast<object>().Select(x =>
			{
				IndexItem ii = new IndexItem();
				ii.Object.IndexName=this.DefaultIndex;
				ii.Object.Id=this.Conventions.IdConvention.GetId(x);
				ii.Object.Type=this.Conventions.TypeNameConvention.GetTypeName(x.GetType());
				ii.Content.Values.AddRange(ProcessObjectToIndex(x));
				return ii;
			}).ToList();

			_indexRepository.SaveIndexItemsAsync(items).Wait();

			return new BulkResult
			{
				Took=666,
				Items=items.Select(x =>
				{
					KeyValuePair<string,object> verPair = x.Content.Values.FirstOrDefault(y => y.Key=="ContentLink.WorkID$$number");
					return new BulkResultItem { Type=x.Object.Type,Index=x.Object.IndexName,Id=x.Object.Id,Version=default(KeyValuePair<string,object>).Equals(verPair)?0:(int)verPair.Value,Ok=true };
				})
			};
		}

		IEnumerable<KeyValuePair<string,object>> ProcessObjectToIndex(object current)
		{
			JsonSerializer js=Serializer.CreateDefault();
			js.ContractResolver=this.Conventions.ContractResolver;
			js.Converters.Insert(0, DateTimeRawConverter.Instance);
			this.Conventions.CustomizeSerializer?.Invoke(js);

			List<KeyValuePair<string,object>> result=new List<KeyValuePair<string,object>>();
			js.Serialize(new FindJsonWriter((propName,value) => result.Add(new KeyValuePair<string,object>(propName, value))),current);

			return result;
		}

		public IndexResult Index(object objectToIndex)
		{
			throw new NotImplementedException();
		}

		public IndexResult Index(object objectToIndex,Action<IndexCommand> commandAction)
		{
			throw new NotImplementedException();
		}

		public IMultiSearch<TSource> MultiSearch<TSource>()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SearchResults<TResult>> MultiSearch<TResult>(IEnumerable<Tuple<SearchRequestBody,Action<SearchCommand>>> searchRequests,Action<MultiSearchCommand> commandAction)
		{
			throw new NotImplementedException();
		}

		public T NewCommand<T>(Func<CommandContext,T> createMethod) where T : Command
		{
			throw new NotImplementedException();
		}

		public ITypeSearch<TSource> Search<TSource>()
		{
			return new Search<TSource,IQuery>(this,delegate (ISearchContext context)
			{
				context.SourceTypes.Add(typeof(TSource));
				/*context.CommandAction = delegate (SearchCommand command)
				{
					this.Conventions.SearchTypeFilter(command, context.SourceTypes);
				};*/

				context.CommandAction=delegate (SearchCommand command)
				{
					((SearchCommandWrap)command).SourceTypes=context.SourceTypes;
					((SearchCommandWrap)command).Context=context;
				};
			});
		}

		public ITypeSearch<TSource> Search<TSource>(Language language)
		{
			return new Search<TSource,IQuery>(this,delegate (ISearchContext context)
			{
				context.SourceTypes.Add(typeof(TSource));
				context.ContentLanguage=language; //it seems it's not used anywhere further in original EpiServer.Find's client
				/*context.CommandAction = delegate (SearchCommand command)
				{
					this.Conventions.SearchTypeFilter(command, context.SourceTypes);
				};*/

				context.CommandAction=delegate (SearchCommand command)
				{
					((SearchCommandWrap)command).SourceTypes=context.SourceTypes;
					((SearchCommandWrap)command).Context=context;
				};
			});
		}

		#region Search
		public SearchResults<TResult> Search<TResult>(SearchRequestBody requestBody,Action<SearchCommand> commandAction)
		{
			SearchCommandWrap searchCommand = new SearchCommandWrap();
			commandAction(searchCommand);

			ISearchResult dbResult;
			if (requestBody.Query==null)
				dbResult=EmptySearchResult.Instance;
			else
			{
				ISearchQueryBuilder sqb=_indexRepository.CreateSearchQueryBuilder();
				IQuery query=requestBody.Query;
				if (query is ConstantScoreQuery constantScoreQuery)
					WriteFilter(constantScoreQuery.Filter,sqb);
				else if ((query is FilteredQuery filteredQuery)&&(filteredQuery.Query is MultiFieldQueryStringQuery multiFieldQueryStringQuery)&&(multiFieldQueryStringQuery.Query is FieldFilterValue fieldFilterValue))
				{
					WriteFilter(filteredQuery.Filter,sqb);
					//WriteFilter(new OrFilter(multiFieldQueryStringQuery.Fields.Select(field=>new TermFilter(field,fieldFilterValue)).ToArray()),sqb);
					WriteFilter(new QueryStringFilter(multiFieldQueryStringQuery.Fields,fieldFilterValue),sqb);
				}
				else
					throw new InvalidOperationException("Unsupported query type");

				sqb.SetRequestedFields(requestBody.Fields);
				sqb.SetScriptFields(requestBody.ScriptFields.Select(x=>new Server.Abstract.ScriptField(x.Name,x.Script,x.Language,x.Parameters.ToDictionary(xi=>xi.Key,xi=>FieldFilterValue2Value(xi.Value)))));
				sqb.SetSkipSize(requestBody.From);
				sqb.SetResultSize(requestBody.Size);
				sqb.AddSort(requestBody.Sort.OfType<Sorting>().Select(x => new SortInfo(x)).ToArray());

				dbResult=sqb.ExecuteAsync().Result;
			}

			string[] scriptFieldNames=requestBody.ScriptFields.Select(x=>x.Name).ToArray();
			int totalCount=dbResult.Count;
			List<SearchHit<TResult>> items=dbResult.Items.Select(x => new SearchHit<TResult>() { Document=(TResult)(object)new ResultJObject(x.Document.Id,x.Document.LanguageName,x.Document.Types,FixScriptFieldValues(x.Document.FieldValues,scriptFieldNames),requestBody.PartialFields.Select(pf=>pf.Name).ToArray()),Id=x.Index.Id,Index=x.Index.Name,Score=x.Index.Score,Highlights=MapHighlights(x.Highlights) }).ToList();
			HitCollection<TResult> hc=new HitCollection<TResult>() { Hits=items,Total=totalCount };
			return new SearchResults<TResult>(new SearchResult<TResult>()
			{
				Shards=new Shards { Total=totalCount,Successful=totalCount,Failed=0 },
				Hits=hc,
				Facets=requestBody.Facets.OfType<TermsFacetRequest>().Select(ProcessTermsFacet).ToFacetResult()
			});
		}

		static Highlights MapHighlights(SearchResultHighlight highlights)
		{
			Highlights result=new Highlights();
			foreach (SearchFieldHighlights item in highlights.Fields)
				result.Add(new FieldHighlights(item.FieldName,item.Highlights));
			return result;
		}

		static IEnumerable<KeyValuePair<string, object>> FixScriptFieldValues(IEnumerable<KeyValuePair<string, object>> fieldValues,string[] scriptFieldNames)
		{
			return fieldValues.Select(x=>(x.Value==null)&&(Array.IndexOf(scriptFieldNames,x.Key)!=-1)
				? new KeyValuePair<string,object>(x.Key,"") : x);
		}

		class SortInfo:ISortInfo
		{
			Sorting _org;
			internal SortInfo(Sorting org)
			{
				_org=org;
			}

			public bool Descendant { get { return _org.Order==SortOrder.Descending; } }

			public string PropertyName { get { return _org.FieldName; } }
		}

		class EmptySearchResult : ISearchResult
		{
			private EmptySearchResult()
			{}

			static SearchResultItem[] _items=new SearchResultItem[0];
			public IEnumerable<SearchResultItem> Items => _items;

			public int Count => 0;

			public void Dispose()
			{}

			static EmptySearchResult _instance = new EmptySearchResult();
			internal static EmptySearchResult Instance => _instance;
		}
		#endregion Search

		public SearchResults<TSource> SearchForType<TSource>(SearchRequestBody requestBody,Action<SearchCommand> commandAction)
		{
			throw new NotImplementedException();
		}

		public ITypeUpdate<TSource> Update<TSource>(DocumentId id)
		{
			throw new NotImplementedException();
		}

		public IndexResult Update<TSource>(DocumentId id,UpdateRequestBody requestBody,Action<UpdateCommand> commandAction)
		{
			throw new NotImplementedException();
		}

		#region FindJsonWriter
		class FindJsonWriter:JsonWriter
		{
			Action<string,object> _writter;
			internal FindJsonWriter(Action<string,object> writter)
			{
				_writter=writter;
			}

			public override void Flush()
			{ }

			static Regex _regex = new Regex(@"(\[[0-9]*\])");
			void WriteValueObj(object value)
			{
				_writter(_regex.Replace(this.Path,""),value);
			}

			#region overrides
			public override void WriteValue(short? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(ushort? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(char? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(byte? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(sbyte? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(decimal? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(DateTime? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(DateTimeOffset? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(Guid? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(TimeSpan? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(byte[] value)
			{
				base.WriteValue(value);
			}

			public override void WriteValue(object value)
			{
				if (value!=null)
					WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(bool? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(double? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(DateTimeOffset value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(ulong? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(string value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(int value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(uint value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(long value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(ulong value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(float value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(float? value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(bool value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(short value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(double value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(char value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(long? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(uint? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(ushort value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(TimeSpan value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(Guid value)
			{
				WriteValueObj(value.ToString());
				base.WriteValue(value);
			}

			public override void WriteValue(int? value)
			{
				if (value.HasValue)
					WriteValueObj(value.Value);
				base.WriteValue(value);
			}

			public override void WriteValue(DateTime value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(sbyte value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(byte value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}

			public override void WriteValue(decimal value)
			{
				WriteValueObj(value);
				base.WriteValue(value);
			}
			#endregion overrides
		}
		#endregion FindJsonWriter

		#region DateTimeRawConverter
		class DateTimeRawConverter:JsonConverter
		{
			internal static DateTimeRawConverter Instance { get; } = new DateTimeRawConverter();

			static Type _typeDt=typeof(DateTime);
			static Type _typeDtNull=typeof(DateTime?);
			public override bool CanConvert(Type objectType)
			{
				return _typeDt.Equals(objectType)||_typeDtNull.Equals(objectType);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				throw new NotImplementedException();
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				if (value is DateTime valDt)
					writer.WriteValue(valDt);
				else if (value is DateTime?)
					writer.WriteValue((DateTime?)value);
				else
					throw new InvalidProgramException();
			}

			internal static object DateTimeToString(DateTime dt)
			{
				return dt.ToString("s");
			}
		}
		#endregion DateTimeRawConverter

		#region WriteFilter
		void WriteFilter(Filter filter,IQueryBuilder qb)
		{
			if (filter is TermFilter tf)
				WriteFilterTerm(tf,qb);
			else if (filter is TermsFilter tsf)
				WriteFilterTerms(tsf,qb);
			else if (filter is AndFilter af)
				WriteFilterAnd(af,qb);
			else if (filter is OrFilter of)
				WriteFilterOr(of,qb);
			else if (filter is NotFilter nf)
				WriteFilterNot(nf,qb);
			else if (filter is ExistsFilter ef)
				WriteFilterExists(ef,qb);
			else if (filter is QueryStringFilter qsf)
				WriteFilterFulltext(qsf,qb);
			else
			{
				Type type = filter.GetType();
				if (type.IsGenericType&&type.GetGenericTypeDefinition().Equals(typeof(RangeFilter<>)))
				{
					WriteFilterRange(filter,qb);
					return;
				}

				throw new InvalidOperationException("Unsupported filter type");
			}
		}

		void WriteFilterTerm(TermFilter filter,IQueryBuilder qb)
		{
			qb.AddTerm(filter.Field,FieldFilterValue2Value(filter.Value));
		}

		void WriteFilterTerms(TermsFilter filter,IQueryBuilder qb)
		{
			qb.AddTerms(filter.Field,filter.Values.Select(FieldFilterValue2Value).ToArray());
		}

		object FieldFilterValue2Value(FieldFilterValue value)
		{
			object val = value.GetType().GetProperty("Value").GetValue(value);
			if (val.GetType().BaseType==typeof(Enum))
				return Convert.ChangeType(val,typeof(int));
			return val;
		}

		void WriteFilterAnd(AndFilter filter,IQueryBuilder qb)
		{
			IQueryBuilder qbIn = qb.AddAnd();
			foreach (Filter item in filter.Filters)
				WriteFilter(item,qbIn);
		}

		void WriteFilterOr(OrFilter filter,IQueryBuilder qb)
		{
			IQueryBuilder qbIn = qb.AddOr();
			foreach (Filter item in filter.Filters)
				WriteFilter(item,qbIn);
		}

		void WriteFilterNot(NotFilter filter,IQueryBuilder qb)
		{
			WriteFilter(filter.Filter,qb.AddNot());
		}

		void WriteFilterExists(ExistsFilter filter,IQueryBuilder qb)
		{
			qb.AddExists(filter.Field);
		}

		void WriteFilterRange(Filter filter,IQueryBuilder qb)
		{
			Type type = filter.GetType();
			string fieldName = (string)type.GetProperty("Field").GetValue(filter);
			object valFrom = type.GetProperty("From").GetValue(filter);
			object valTo = type.GetProperty("To").GetValue(filter);
			bool includeLower = ((bool?)type.GetProperty("IncludeLower").GetValue(filter))??true;
			bool includeUpper = ((bool?)type.GetProperty("IncludeUpper").GetValue(filter))??true;

			qb.AddRange(fieldName,valFrom,includeLower,valTo,includeUpper);
		}

		void WriteFilterFulltext(QueryStringFilter filter,IQueryBuilder qb)
		{
			qb.AddFullText(FieldFilterValue2Value(filter.Value) as string, filter.Fields.Select(x=> {
				int pos = x.IndexOf("$$");
				if (pos != -1)
				{
					pos = x.IndexOf(".", pos);
					if (pos != -1)
						x = x.Substring(0, pos);
				}
				return x;
			}).Distinct().ToArray());
		}
		#endregion WriteFilter

		#region ProcessTermsFacet
		TermsFacet ProcessTermsFacet(TermsFacetRequest facet)
		{
			IFacetQueryBuilder fqb = _indexRepository.CreateFacetQueryBuilder();
			fqb.SetField(facet.Field);
			fqb.SetResultSize(facet.Size);
			IFacetResult res = fqb.ExecuteAsync().Result;
			return new TermsFacet { Name=facet.Name,Terms=res.Terms.Select(x => new TermCount { Term=x.Value.ToString(),Count=x.Count }).ToList(),Missing=res.Missing };
		}

		public IndexResult Update<TSource>(DocumentId id, UpdateRequestBody requestBody)
		{
			throw new NotImplementedException();
		}

		public IndexResult Update<TSource>(DocumentId id, UpdateRequestBody requestBody, LanguageRouting languageRouting)
		{
			throw new NotImplementedException();
		}

		public IndexResult Update<TSource>(DocumentId id, UpdateRequestBody requestBody, LanguageRouting languageRouting, Action<UpdateCommand> commandAction)
		{
			throw new NotImplementedException();
		}

		public BulkResult Index(IEnumerable objectsToIndex, bool deleteLanguageRoutingDuplicatesOnIndex)
		{
			/*if (deleteLanguageRoutingDuplicatesOnIndex)
			{
				BulkDeleteAction item = new BulkDeleteAction(this.DefaultIndex, this.GetTypeName(item2), id)
				{
					ActionAndMeta = { LanguageRouting = null }
				};
				list.Add(item);
			}*/

			return Index(objectsToIndex);
		}

		public TSource Get<TSource>(DocumentId id, LanguageRouting languageRouting)
		{
			throw new NotImplementedException();
		}

		public TSource Get<TSource>(DocumentId id, LanguageRouting languageRouting, Action<GetCommand<TSource>> commandAction)
		{
			throw new NotImplementedException();
		}

		public GetResult<TSource> GetWithMeta<TSource>(DocumentId id, LanguageRouting languageRouting, Action<GetCommand<TSource>> commandAction)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<GetResult<TSource>> Get<TSource>(IEnumerable<Tuple<DocumentId, LanguageRouting>> ids)
		{
			throw new NotImplementedException();
		}

		public DeleteResult Delete<T>(DocumentId id, LanguageRouting languageRouting)
		{
			throw new NotImplementedException();
		}

		public DeleteResult Delete<T>(DocumentId id, LanguageRouting languageRouting, Action<DeleteCommand> commandAction)
		{
			throw new NotImplementedException();
		}

		public DeleteResult Delete(Type type, DocumentId id, LanguageRouting languageRouting, Action<DeleteCommand> commandAction)
		{
			throw new NotImplementedException();
		}
		#endregion ProcessTermsFacet

		#region SearchCommandWrap & DeleteByQueryCommandWrap
		class SearchCommandWrap:SearchCommand
		{
			internal SearchCommandWrap() : base(null)
			{
			}

			internal IList<Type> SourceTypes { get; set; }
			internal ISearchContext Context { get; set; }
		}

		class DeleteByQueryCommandWrap:DeleteByQueryCommand
		{
			public DeleteByQueryCommandWrap() : base(null)
			{ }

			public override DeleteByQueryResult Execute()
			{
				DeleteByQueryResult res = base.Execute();
				return res;
			}

			protected override TResult GetResponse<TResult>(IJsonRequest request)
			{
				TResult res = base.GetResponse<TResult>(request);
				return res;
			}
		}
		#endregion SearchCommandWrap & DeleteByQueryCommandWrap

		#region ResultJObject
		class ResultJObject:JObject
		{
			internal ResultJObject(int contentLinkId,string lang,IEnumerable<string> types,IEnumerable<KeyValuePair<string,object>> values,IEnumerable<string> partialFields)
			{
				this["___types"] = new JArray(types.Select(x=>new JValue(x)).ToArray());
				this["ContentLink.ID$$number"]=contentLinkId;
				this["ContentLink.ProviderName$$string"]="";
				this["Language.Name$$string"]=lang;

				foreach (KeyValuePair<string,object> item in values)
				{
					object val=item.Value;
					if (val is DateTime dt)
						val=DateTimeRawConverter.DateTimeToString(dt);
					this[item.Key]=new JValue(val);
				}

				foreach (string item in partialFields)
					this[item.EndsWith(".*")?item.Substring(0,item.Length-2):item]=new JObject();
			}
		}
		#endregion ResultJObject

		#region QueryStringFilter
		class QueryStringFilter : Filter
		{
			internal IList<string> Fields;
			internal FieldFilterValue Value;

			public QueryStringFilter(IList<string> fields, FieldFilterValue fieldFilterValue)
			{
				Fields = fields;
				Value = fieldFilterValue;
			}
		}
		#endregion QueryStringFilter
	}

	#region FacetsExtensions
	static class FacetsExtensions
	{
		internal static FacetResults ToFacetResult(this IEnumerable<Facet> facets)
		{
			FacetResults result = new FacetResults();
			foreach (Facet item in facets)
				result.Add(item);
			return result;
		}
	}
	#endregion FacetsExtensions
}
