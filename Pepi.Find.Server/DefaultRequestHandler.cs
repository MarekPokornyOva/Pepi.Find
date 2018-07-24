#region using
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pepi.Find.Server.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#endregion using

namespace Pepi.Find.Server
{
	public class DefaultRequestHandler:IRequestHandler
	{
		#region .ctor
		IIndexRepository _indexRepository;
		RequestHandlerOptions _options;
		public DefaultRequestHandler(IIndexRepository indexRepository,RequestHandlerOptions options)
		{
			_indexRepository=indexRepository;
			_options=options;
		}

		public DefaultRequestHandler(IIndexRepository indexRepository) : this(indexRepository,RequestHandlerOptions.Unlimited)
		{ }
		#endregion .ctor

		#region ProcessRequest
		public async Task ProcessRequestAsync(IHandlerContext context)
		{
			IHandlerRequest req = context.Request;
			IHandlerResponse res = context.Response;
			//string clientInfo = req.UserAgent; //EPiServer-Find-NET-API/12.2.4.0
			string url = req.RawUrl;

			if (url.EndsWith("/_bulk"))
				await ProcessBulkAsync(res,GetTextReader(req.Body));
			else if (req.RawUrl.EndsWith("/_query"))
				await ProcessQueryAsync(res,GetTextReader(req.Body));
			/*else if (req.RawUrl.EndsWith("/_config"))
			{
				 SaveRequest(url,new StreamReader(req.InputStream).ReadToEnd());

				 Console.WriteLine("404 - Not found");
				 context.Response.StatusCode=404;
				 context.Response.OutputStream.Close();
			}*/
			else if (req.RawUrl.EndsWith("/_search"))
				await ProcessSearchAsync(res,GetTextReader(req.Body));
			else
				res.StatusCode=404;
			res.Body.Dispose();
		}

		TextReader GetTextReader(Stream source)
		{
			return new StreamReader(source);
		}
		#endregion ProcessRequest

		#region ProcessBulk
		async Task ProcessBulkAsync(IHandlerResponse response,TextReader content)
		{
			List<IndexItem> bulkItems = new List<IndexItem>();
			IndexItemObject @object = null;
			List<KeyValuePair<string,object>> contentValuesItem = null;
			ReadJson(content,jr =>
			{
				if (jr.Path.StartsWith("index."))
				{
					string fieldName = jr.Path.Substring(6);
					if (fieldName=="_index")
					{
						IndexItem bi;
						bulkItems.Add(bi=new IndexItem());
						@object=bi.Object;
						@object.IndexName=(string)jr.Value;
						contentValuesItem=bi.Content.Values;
					}
					else
						switch (fieldName)
						{
							case "_type":
								@object.Type=(string)jr.Value;
								break;
							case "_id":
								@object.Id=(string)jr.Value;
								break;
						}
				}
				else
				{
					string propName=RemovePathIndex(jr.Path);
					if (propName=="ContentLink.WorkID$$number")
						@object.Version=(int)Convert.ChangeType(jr.Value,typeof(int));
					contentValuesItem.Add(new KeyValuePair<string,object>(propName,jr.Value));
				}
			});

			await _indexRepository.SaveIndexItemsAsync(bulkItems);

			response.StatusCode=200;

			using (TextWriter tw = new StreamWriter(response.Body))
			using (JsonWriter jw = new JsonTextWriter(tw))
				using (jw.WriteObject())
				{
					jw.WriteProperty("took",666); //HACK: should be stopwatch time of something
					using (jw.WriteArray("items"))
						foreach (IndexItem item in bulkItems)
							using (jw.WriteObject())
							using (jw.WriteObject("index"))
								jw
									  .WriteProperty("_index",item.Object.IndexName)
									  .WriteProperty("_type",item.Object.Type)
									  .WriteProperty("_id",item.Object.Id)
									  .WriteProperty("_version",item.Object.Version)
									  .WriteProperty("ok",true);
				}
		}
		#endregion ProcessBulk

		#region ProcessQuery
		async Task ProcessQueryAsync(IHandlerResponse response,TextReader content)
		{
			JObject o = (JObject)JsonSerializer.Create().Deserialize(content,typeof(object));
			JObject queryFilter = o.SelectToken("filtered.query.constant_score.filter") as JObject;
			JObject filter = o.SelectToken("filtered.filter") as JObject;

			if ((queryFilter==null)||(filter==null))
			{
				response.StatusCode=400;
				response.Body.Dispose();
				return;
			}

			using (ISearchResult data = await ParseQueryFilter(null,null,null,null,null,null,null,queryFilter,filter).ExecuteAsync())

			//serialize to parsed as EPiServer.Find.Api.DeleteByQueryResult; Indices are read by EPiServer.Find.Api.IndicesResultConverter
			//{"ok":true,"_indices":{"<index_name>":{"_shards":{"total":2,"successful":2,"failed":0}}}}

			using (TextWriter tw = new StreamWriter(response.Body))
			using (JsonWriter jw = new JsonTextWriter(tw))
			{
				int dataCount = data.Count;
				using (jw.WriteObject())
				{
					jw.WriteProperty("ok",true);

					using (jw.WriteObject("_indices"))
					using (jw.WriteObject("zelvicek_helen")) //HACK: index name has to be taken from DB
					using (jw.WriteObject("_shards"))
						jw
							  .WriteProperty("total",dataCount)
							  .WriteProperty("successful",dataCount)
							  .WriteProperty("failed",0);
				}
			}
		}
		#endregion ProcessQuery

		#region ProcessSearch
		async Task ProcessSearchAsync(IHandlerResponse response,TextReader content)
		{
			/*ReadJson(content, jr =>
		{
		});*/

			JObject o = (JObject)JsonSerializer.Create().Deserialize(content,typeof(object));
			List<JObject> queryParts = new List<JObject>();
			Func<JToken,JObject> AddQueryPart = new Func<JToken,JObject>(qp => { JObject jo = qp as JObject; if (jo!=null) queryParts.Add(jo); return jo; });

			JObject filter = AddQueryPart(o.SelectToken("query.filtered.filter"));
			AddQueryPart(o.SelectToken("query.filtered.query.constant_score.filter"));
			JToken jtb = o.SelectToken("query.filtered.query.filtered");
			if (jtb!=null)
			{
				AddQueryPart(jtb.SelectToken("query"));
				AddQueryPart(jtb.SelectToken("filter"));
			}

			JValue skip = o.SelectToken("from") as JValue;
			JValue take = o.SelectToken("size") as JValue;

			if ((filter==null)||(!TryTranslateSkipTake(skip,out long? skipVal))||(!TryTranslateSkipTake(take,out long? takeVal))
				 ||((_options.SearchMaxCount.HasValue)&&((!takeVal.HasValue)||(takeVal>_options.SearchMaxCount.Value)))
				 )
			{
				Console.WriteLine("400 - Bad request");
				response.StatusCode=400;
				response.Body.Dispose();
				return;
			}

			if ((_options.SearchDefaultCount.HasValue)&&(!takeVal.HasValue))
				takeVal=_options.SearchDefaultCount;

			SortInfo[] sortFields=(o.SelectToken("sort") as JArray)?.Values().Select(x=>
			{
				JProperty jProp=(JProperty)x;
				return new SortInfo { PropertyName=jProp.Name,Descendant=(jProp.Value.SelectToken("order") as JValue)?.Value as string=="desc" };
			}).ToArray();
			string[] requestedFields=(jtb.SelectToken("query.query_string.fields") as JArray)?.Values().Select(x=>((JValue)x).Value as string).Distinct().ToArray();
			IEnumerable<ScriptField> scriptFields=(o.SelectToken("script_fields") as JObject)?.Properties()?.Select(x=>
			{
				JObject val=(JObject)x.Value;
				return new ScriptField(x.Name, 
					(val["script"] as JValue)?.Value as string, 
					(val["lang"] as JValue)?.Value as string, 
					(val["params"] as JObject)?.Properties().ToDictionary(par=>par.Name,par=>(par.Value as JValue)?.Value));
			})?.ToArray();
			string highlightsQuery=(jtb.SelectToken("query.query_string.query") as JValue)?.Value as string;
			IEnumerable<HighlightFieldRequest> highlightFields=(o.SelectToken("highlight.fields") as JObject)?.Properties()?.Select(x=>
			{
				JObject val=(JObject)x.Value;
				return new HighlightFieldRequest(x.Name,(val["pre_tags"] as JArray)?.Values().Select(tv=>(tv as JValue)?.Value as string),(val["post_tags"] as JArray)?.Values().Select(tv=>(tv as JValue)?.Value as string));
			}).ToArray();
			IEnumerable<string> partialFields=(o.SelectToken("partial_fields") as JObject)?.Properties()?.Select(x=>x.Name).ToArray();

			IEnumerable<JProperty> facets = (o.SelectToken("facets") as JObject)?.Properties();

			using (ISearchResult data = await ParseQueryFilter(skipVal,takeVal,sortFields,requestedFields,scriptFields,highlightsQuery,highlightFields,queryParts.Where(x => x!=null).ToArray()).ExecuteAsync())
			using (TextWriter tw = new StreamWriter(response.Body))
			using (JsonWriter jw = new JsonTextWriter(tw))
			{
				int dataCount = data.Count;
				using (jw.WriteObject())
				{
					jw
						 .WriteProperty("took",666)
						 .WriteProperty("timed_out",false);

					using (jw.WriteObject("_shards"))
						jw
							 .WriteProperty("total",dataCount)
							 .WriteProperty("successful",dataCount)
							 .WriteProperty("failed",0);

					if ((facets!=null)&&(facets.Any()))
						using (jw.WriteObject("facets"))
							foreach (JProperty fProp in facets)
								using (IFacetResult fItem = await ResolveFacetAsync(fProp.Value as JObject,_options.FacetDefaultCount))
									if (fItem!=null)
										using (jw.WriteObject(fProp.Name))
										{
											jw
												 .WriteProperty("_type","terms")
												 .WriteProperty("missing",fItem.Missing)
												 .WriteProperty("total",fItem.Total)
												 .WriteProperty("other",fItem.Other);

											using (jw.WriteArray("terms"))
												foreach (FacetResultTermItem termItem in fItem.Terms)
													using (jw.WriteObject())
														jw
															 .WriteProperty("term",termItem.Value.ToString())
															 .WriteProperty("count",termItem.Count);
										}

					using (jw.WriteObject("hits"))
					{
						jw
							 .WriteProperty("total",dataCount)
							 .WritePropertyNull("max_score");

						using (jw.WriteArray("hits"))
							foreach (SearchResultItem item in data.Items)
								using (jw.WriteObject())
								{
									jw
											  .WriteProperty("_index",item.Index.Name)
											  .WriteProperty("_type",item.Index.ContentTypeName)
											  .WriteProperty("_id",item.Index.Id);
									if (item.Index.Score.HasValue)
										jw.WriteProperty("_score",item.Index.Score.Value);
									else
										jw.WritePropertyNull("_score");

									using (jw.WriteObject("fields"))
									{
										jw
											.WriteProperty("ContentLink.ID$$number",item.Document.Id)
											.WriteProperty("PageLink.ID$$number",item.Document.Id)
											.WriteProperty("Language.Name$$string",item.Document.LanguageName);

										using (jw.WriteArray("___types"))
											foreach (string type in item.Document.Types)
												jw.WriteValue(type);

										foreach (ScriptField scriptField in scriptFields)
											jw.WriteProperty(scriptField.Name,item.Document.FieldValues.First(x=>x.Key==scriptField.Name).Value?.ToString()??"");

										foreach (string partialField in partialFields)
											using (jw.WriteObject(partialField.EndsWith(".*")?partialField.Substring(0,partialField.Length-2):partialField))
												;
									}

									using (jw.WriteObject("highlight"))
										foreach (SearchFieldHighlights srh in item.Highlights.Fields)
											using (jw.WriteArray(srh.FieldName))
												foreach (string value in srh.Highlights)
													jw.WriteValue(value);
								}
					}
				}
			}
		}

		static bool TryTranslateSkipTake(JValue jval,out long? value)
		{
			value=null;
			if (jval==null)
				return true;

			object val = jval.Value;
			if (!(val is long))
				return false;

			value=(long)val;
			return true;
		}

		async Task<IFacetResult> ResolveFacetAsync(JObject jObject,int? resultSize)
		{
			string fieldName = (jObject.SelectToken("terms.field") as JValue)?.Value as string;
			if (fieldName==null)
				return null;

			IFacetQueryBuilder qb = _indexRepository.CreateFacetQueryBuilder();
			qb.SetField(fieldName);
			qb.SetResultSize(resultSize);
			return await qb.ExecuteAsync();
		}
		#endregion ProcessSearch

		#region ReadJson
		static void ReadJson(TextReader reader,Action<JsonReader> valueCallback)
		{
			using (reader)
			using (JsonTextReader jr = new MultiRootJsonTextReader(reader))
			{
				while (jr.Read())
				{
					if ((jr.TokenType==JsonToken.Integer)||(jr.TokenType==JsonToken.String)||(jr.TokenType==JsonToken.Boolean)||(jr.TokenType==JsonToken.Date)||(jr.TokenType==JsonToken.Float))
					{
						if (valueCallback!=null)
							valueCallback(jr);
					}
					/*else if (jr.TokenType == JsonToken.Null)
					{
						if (valueCallback != null)
							valueCallback(jr);
					}*/
				}
			}
		}

		static Regex _pathIndexRegex = new Regex("\\[\\d+\\]");
		static string RemovePathIndex(string path)
		{
			return _pathIndexRegex.Replace(path,"");
		}
		#endregion ReadJson

		#region ParseQueryFilter & WriteFilter
		ISearchQueryBuilder ParseQueryFilter(long? skip,long? take,SortInfo[] sort,string[] requestedFields,IEnumerable<ScriptField> scriptFields,string highlightsQuery,IEnumerable<HighlightFieldRequest> highlightsFields,params JObject[] filters)
		{
			ISearchQueryBuilder qb = _indexRepository.CreateSearchQueryBuilder();
			foreach (JObject filter in filters)
				WriteFilter(filter,qb);

			if (requestedFields!=null)
				qb.SetRequestedFields(requestedFields);
			if (scriptFields!=null)
				qb.SetScriptFields(scriptFields);
			qb.SetSkipSize(skip);
			qb.SetResultSize(take);
			if (sort!=null)
				qb.AddSort(sort);
			if (highlightsFields!=null)
				qb.SetHighlights(highlightsQuery,highlightsFields);

			return qb;
		}

		void WriteFilter(JObject filter,IQueryBuilder qb)
		{
			JProperty p = filter.Properties().First();

			switch (p.Name)
			{
				case "term":
					WriteFilterTerm(p.Value,qb);
					break;
				case "terms":
					WriteFilterTerms(p.Value,qb);
					break;
				case "and":
					WriteFilterAnd(p.Value,qb);
					break;
				case "or":
					WriteFilterOr(p.Value,qb);
					break;
				case "not":
					WriteFilterNot(p.Value,qb);
					break;
				case "range":
					WriteFilterRange(p.Value,qb);
					break;
				case "exists":
					WriteFilterExists(p.Value,qb);
					break;
				case "query_string":
					WriteFilterFulltext(p.Value,qb);
					break;
				default:
					throw new InvalidOperationException("Unsupported filter type");
			}
		}

		private void WriteFilterTerm(JToken filter,IQueryBuilder qb)
		{
			JProperty term = (JProperty)((JObject)filter).First;
			qb.AddTerm(term.Name,((JValue)term.Value).Value);
		}

		private void WriteFilterTerms(JToken filter,IQueryBuilder qb)
		{
			JProperty term = (JProperty)((JObject)filter).First;
			qb.AddTerms(term.Name,((JArray)term.Value).Values().Select(x => ((JValue)x).Value).ToArray());
		}

		private void WriteFilterAnd(JToken filter,IQueryBuilder qb)
		{
			IQueryBuilder qbAnd = qb.AddAnd();
			foreach (JObject item in (JArray)filter)
				WriteFilter(item,qbAnd);
		}

		private void WriteFilterOr(JToken filter,IQueryBuilder qb)
		{
			IQueryBuilder qbOr = qb.AddOr();
			foreach (JObject item in (JArray)filter)
				WriteFilter(item,qbOr);
		}

		private void WriteFilterNot(JToken filter,IQueryBuilder qb)
		{
			WriteFilter((JObject)filter.SelectToken("filter"),qb.AddNot());
		}

		private void WriteFilterRange(JToken filter,IQueryBuilder qb)
		{
			JProperty mainProp = (JProperty)filter.First;
			JObject propCont = (JObject)mainProp.Value;

			string fieldName = mainProp.Name;

			JProperty propFrom = propCont.Property("from");
			bool propFromDefined = propFrom!=null;
			object valFrom = propFromDefined ? ((JValue)propFrom.Value).Value : null;

			JProperty propTo = propCont.Property("to");
			bool propToDefined = propTo!=null;
			object valTo = propToDefined ? ((JValue)propTo.Value).Value : null;

			JProperty propIncludeLower = propCont.Property("include_lower");
			bool propIncludeLowerDefined = propIncludeLower!=null;
			bool includeLower = propIncludeLowerDefined&&(bool)propIncludeLower.Value;

			JProperty propIncludeUpper = propCont.Property("include_upper");
			bool propIncludeUpperDefined = propIncludeUpper!=null;
			bool includeUpper = propIncludeUpperDefined&&(bool)propIncludeUpper.Value;

			qb.AddRange(mainProp.Name,valFrom,includeLower,valTo,includeUpper);
		}

		private void WriteFilterExists(JToken filter,IQueryBuilder qb)
		{
			qb.AddExists((string)((JObject)filter).Property("field").Value);
		}

		private void WriteFilterFulltext(JToken filter,IQueryBuilder qb)
		{
			string searchWord = (string)((JObject)filter).Property("query").Value;
			JArray inFieldsArr = ((JObject)filter).Property("fields")?.Value as JArray;
			string[] inFields = null;
			if (inFieldsArr!=null)
			{
				string analyzer = "."+(string)((JObject)filter).Property("analyzer").Value;
				int analyzerLength = analyzer.Length;
				inFields=inFieldsArr?.Values().Select(x => (x as JValue)?.Value as string).Where(x => x!=null).Where(x => x.EndsWith(analyzer)).Select(x => x.Substring(0,x.Length-analyzerLength)).ToArray();
			}

			qb.AddFullText(searchWord,inFields);
		}
		#endregion ParseQueryFilter & WriteFilter

		#region SortInfo
		class SortInfo:ISortInfo
		{
			public string PropertyName { get; internal set; }
			public bool Descendant { get; internal set; }
		}
		#endregion SortInfo
	}
}
