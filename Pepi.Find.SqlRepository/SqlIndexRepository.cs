﻿#region using
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using System.Text;
using Pepi.Find.Server.Abstract;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
#endregion using

namespace Pepi.Find.SqlRepository
{
	public class SqlIndexRepository:IIndexRepository, IDisposable
	{
		#region .ctor & Disponse
		static SemaphoreSlim _dbLock = new SemaphoreSlim(1);
		SqlConnection _dbConnection;
		public SqlIndexRepository(string connectionString)
		{
			_dbConnection=new SqlConnection(connectionString);
			_dbConnection.Open();
		}

		public void Dispose()
		{
			_dbConnection.Dispose();
			_dbLock.Dispose();
		}
		#endregion .ctor & Disponse

		#region SaveIndexItemsAsync
		public async Task SaveIndexItemsAsync(IEnumerable<IndexItem> indexItems)
		{
			foreach (IndexItem item in indexItems)
			{
				SqlTransaction trans = null;
				await _dbLock.WaitAsync();
				try
				{
					trans=_dbConnection.BeginTransaction();

					int contentId = (int)Convert.ChangeType(item.Content.Values.Find(x => x.Key=="ContentLink.ID$$number").Value,typeof(int));

					using (IDbCommand cmd = _dbConnection.CreateCommand())
					{
						cmd.Transaction=trans;
						cmd.CommandText=$"delete ContentIndex where IdContent=@IdContent";
						AddParm(cmd,"@IdContent",contentId);
						cmd.ExecuteNonQuery();
					}

					IEnumerable<KeyValuePair<string,object>> valuesToSave = (new[] { new KeyValuePair<string,object>("!!Index.Id",item.Object.Id),new KeyValuePair<string,object>("!!Index.Name",item.Object.IndexName),new KeyValuePair<string,object>("!!Index.Type",item.Object.Type) })
						 .Concat(item.Content.Values);
					/*foreach (KeyValuePair<string,object> value in valuesToSave)
						using (SqlCommand cmd = _dbConnection.CreateCommand())
						{
							string valFieldName = GetValueFieldNameByValue(value.Value);
							cmd.Transaction=trans;
							cmd.CommandText=string.Concat("insert ContentIndex (IdContent, PropertyName, Type, ",valFieldName,") values (@IdContent, @PropertyName, @Type, @Value)");
							AddParm(cmd,"@IdContent",contentId);
							AddParm(cmd,"@PropertyName",value.Key);
							AddParm(cmd,"@Type",valFieldName);
							AddParm(cmd,"@Value",value.Value);
							await cmd.ExecuteNonQueryAsync();
						}*/
					await new SqlBulkCopy(_dbConnection,SqlBulkCopyOptions.Default,trans)
					{
						BatchSize=100,
						DestinationTableName="ContentIndex"
					}
						.WriteToServerAsync(new IndexValuesDataReader(contentId,valuesToSave,GetValueFieldNameByValue));

					trans.Commit();
					trans.Dispose();
				}
				catch //(Exception ex)
				{
					//mark the contentId as not indexed - it might be returned as failed to indexing client
					if (trans!=null)
					{
						trans.Rollback();
						trans.Dispose();
					}
					throw;
				}
				finally
				{
					_dbLock.Release();
				}
			}
		}
		#endregion SaveIndexItemsAsync

		#region AddParm
		void AddParm(IDbCommand cmd,string name,object value)
		{
			IDbDataParameter parm = cmd.CreateParameter();
			parm.ParameterName=name;
			parm.Value=value;
			if (value is DateTime)
				((SqlParameter)parm).SqlDbType=SqlDbType.DateTime2;
			cmd.Parameters.Add(parm);
		}
		#endregion AddParm

		#region GetValueFieldName
		internal static string GetValueFieldNameByValue(object value)
		{
			Type valType=value.GetType();
			if ((valType==typeof(string))||(valType==typeof(Guid)))
				return "ValueString";
			if ((valType==typeof(int))||(valType==typeof(long)))
				return "ValueInt";
			if ((valType==typeof(float))||(valType==typeof(double)))
				return "ValueFloat";
			if (valType==typeof(DateTime))
				return "ValueDate";
			if (valType==typeof(bool))
				return "ValueBool";
			throw new Exception("Unsupported property type");
		}

		internal static string GetValueFieldNameByName(string fieldName)
		{
			string result=GetValueFieldNameByNameInternal(fieldName);
			return result??throw new Exception("Undetermined property type");
		}

		static string GetValueFieldNameByNameInternal(string fieldName)
		{
			int pos=fieldName.IndexOf("$$");
			if (pos==-1)
				return null;
			fieldName=ConsolidateFieldType(fieldName.Substring(pos+2));

			if (fieldName=="string")
				return "ValueString";
			if (fieldName=="number")
				return "ValueInt";
			/*if (fieldName == typeof(float))
				return "ValueFloat";*/
			if (fieldName=="date")
				return "ValueDate";
			if (fieldName=="bool")
				return "ValueBool";
			return null;
		}

		static bool TryGetValueFieldNameByName(string fieldName, out string result)
		{
			result= GetValueFieldNameByNameInternal(fieldName);
			return result!=null;
		}

		static string ConsolidateFieldType(string type)
		{
			int pos=type.IndexOf(".");
			return pos==-1?type:type.Substring(0,pos);
		}

		static string NormalizeValueFieldName(string fieldName)
		{
			int pos=fieldName.IndexOf("$$");
			if (pos==-1)
				return fieldName;
			pos=fieldName.IndexOf(".");
			return pos==-1?fieldName:fieldName.Substring(0,pos);
		}
		#endregion GetValueFieldName

		#region DataReaderEnumerable
		class DataReaderEnumerable<T>:IEnumerable<T>
		{
			IDataReader _dr;
			Action _disp;
			Func<T> _materializer;
			internal DataReaderEnumerable(IDataReader dr,Action disp,Func<T> materializer) { _dr=dr; _disp=disp; _materializer=materializer; }

			public IEnumerator<T> GetEnumerator() => new DataReaderEnumerator<T>(_dr,_disp,_materializer);
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		class DataReaderEnumerator<T>:IEnumerator<T>
		{
			IDataReader _dr;
			Action _disp;
			Func<T> _materializer;
			internal DataReaderEnumerator(IDataReader dr,Action disp,Func<T> materializer) { _dr=dr; _disp=disp; _materializer=materializer; }

			T _val;
			bool _valTaken;
			public T Current { get { if (!_valTaken) { _val=_materializer(); _valTaken=true; } return _val; } }

			object IEnumerator.Current => Current;

			public void Dispose() => _disp();
			public bool MoveNext() { _valTaken=false; return _dr.Read(); }
			public void Reset() => throw new NotImplementedException();
		}
		#endregion DataReaderEnumerator

		#region IndexValuesDataReader
		class IndexValuesDataReader:DbDataReader
		{
			int _idContent;
			IEnumerator<KeyValuePair<string,object>> _values;
			Func<object,string> GetValueFieldNameByValue;
			string _valFieldName;
			int _valFieldInd;

			public IndexValuesDataReader(int idContent,IEnumerable<KeyValuePair<string,object>> values,Func<object,string> GetValueFieldNameByValue)
			{
				_idContent=idContent;
				_values=values.GetEnumerator();
				this.GetValueFieldNameByValue=GetValueFieldNameByValue;
			}

			public override int FieldCount => 9;

			public override object GetValue(int i)
			{
				if (i==1)
					return _idContent;
				if (i==2)
					return _values.Current.Key;
				if (i==3)
					return _valFieldName;
				return _valFieldInd==i ? _values.Current.Value : DBNull.Value;
			}

			public override bool IsDBNull(int i)
			{
				return (i>3)&&(_valFieldInd!=i);
			}

			static string[] _dataFieldNames = new[] { "ValueString","ValueInt","ValueFloat","ValueDate","ValueBool" };
			public override bool Read()
			{
				bool result = _values.MoveNext();
				if (result)
				{
					_valFieldName=GetValueFieldNameByValue(_values.Current.Value);
					_valFieldInd=Array.IndexOf(_dataFieldNames,_valFieldName)+4;
				}
				return result;
			}

			#region not implemented members
			public override object this[int ordinal] => throw new NotImplementedException();

			public override object this[string name] => throw new NotImplementedException();

			public override int Depth => throw new NotImplementedException();

			public override bool HasRows => throw new NotImplementedException();

			public override bool IsClosed => throw new NotImplementedException();

			public override int RecordsAffected => throw new NotImplementedException();

			public override bool GetBoolean(int ordinal) => throw new NotImplementedException();

			public override byte GetByte(int ordinal) => throw new NotImplementedException();

			public override long GetBytes(int ordinal,long dataOffset,byte[] buffer,int bufferOffset,int length)
				 => throw new NotImplementedException();

			public override char GetChar(int ordinal) => throw new NotImplementedException();

			public override long GetChars(int ordinal,long dataOffset,char[] buffer,int bufferOffset,int length)
				 => throw new NotImplementedException();

			public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();

			public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();

			public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();

			public override double GetDouble(int ordinal) => throw new NotImplementedException();

			public override IEnumerator GetEnumerator() => throw new NotImplementedException();

			public override Type GetFieldType(int ordinal) => throw new NotImplementedException();

			public override float GetFloat(int ordinal) => throw new NotImplementedException();

			public override Guid GetGuid(int ordinal) => throw new NotImplementedException();

			public override short GetInt16(int ordinal) => throw new NotImplementedException();

			public override int GetInt32(int ordinal) => throw new NotImplementedException();

			public override long GetInt64(int ordinal) => throw new NotImplementedException();

			public override string GetName(int ordinal) => throw new NotImplementedException();

			public override int GetOrdinal(string name) => throw new NotImplementedException();

			public override string GetString(int ordinal) => throw new NotImplementedException();

			public override int GetValues(object[] values) => throw new NotImplementedException();

			public override bool NextResult() => throw new NotImplementedException();
			#endregion not implemented members
		}
		#endregion IndexValuesDataReader

		#region SearchResult
		class SearchResult:ISearchResult
		{
			PreparedCommand _preparedCommand;
			SqlConnection _dbConnection;
			string[] _fieldNames;
			Tuple<string,string,string>[] _highlightFields;
			string _highlightQuery;
			bool _loaded;
			internal SearchResult(PreparedCommand preparedCommand,SqlConnection dbConnection,string[] fieldNames,Tuple<string,string,string>[] highlightFields,string highlightQuery)
			{
				_preparedCommand=preparedCommand;
				_dbConnection=dbConnection;
				_fieldNames=fieldNames;
				_highlightFields=highlightFields;
				_highlightQuery=highlightQuery;
			}

			void Load()
			{
				_dbLock.WaitAsync().Wait();
				SqlCommand cmd = null;
				PeekableDataReader dr = null;
				try
				{
					cmd=CreateCommand(_dbConnection,_preparedCommand);

					dr=new PeekableDataReader(cmd.ExecuteReaderAsync().Result);
					Dictionary<string,int> fieldsMapping=_fieldNames.ToDictionary(x=>x,x=>dr.GetOrdinal(x));
					Tuple<string,string,string,int>[] hlFieldsMapping=_highlightFields.Select(x=>new Tuple<string,string,string,int>(x.Item1,x.Item2,x.Item3,dr.GetOrdinal(SearchQueryBuilder.GetHiLightFieldName(x.Item1)))).ToArray();
					StringBuilder sb=new StringBuilder();
					Regex hiLightRegex=null;
					_items=new DataReaderEnumerable<SearchResultItem>(dr,new Action(() => { dr.Dispose(); cmd.Dispose(); if (_dbLock.CurrentCount==0) _dbLock.Release(); }),() =>
					{
						SearchResultItem sri=new SearchResultItem();
						sri.Index.Name=dr.IsDBNull(4)?"":dr.GetString(4);
						sri.Index.ContentTypeName=dr.IsDBNull(5)?"":dr.GetString(5);
						sri.Index.Id=dr.IsDBNull(3)?"":dr.GetString(3);
						sri.Document.Id=dr.GetInt32(0);
						sri.Document.LanguageName=dr.GetString(1);
						sri.Document.Types=dr.GetString(6).Split((char)13);
						sri.Document.FieldValues=_fieldNames.Select(x=> { object val=dr.GetValue(fieldsMapping[x]); if (val==DBNull.Value) val=null; else if (val is int intVal) val=(long)intVal; return new KeyValuePair<string,object>(x,val); }).ToArray();

						sri.Highlights.Fields=hlFieldsMapping.Select(hf=>
						{
							string hlVal;
							if ((dr.IsDBNull(hf.Item4))||((hlVal=dr.GetString(hf.Item4)).Length==0))
								return null;
							sb.Clear();
							if (hiLightRegex==null)
								hiLightRegex=new Regex($@"[\w]*{Regex.Escape(_highlightQuery)}[\w]*",RegexOptions.IgnoreCase);
							ProcessHighlight(hlVal,hf,sb,hiLightRegex);
							if (sb.Length==0)
								return null;
							return new SearchFieldHighlights { FieldName=hf.Item1,Highlights=new string[] { sb.ToString() } };
						}).Where(x=>x!=null).ToArray();

						return sri;
					});
					_count=dr.Peek()?dr.GetInt32(2):0;
				}
				catch
				{
					if (dr!=null)
						dr.Dispose();
					if (cmd!=null)
						cmd.Dispose();
					_dbLock.Release();
					throw;
				}

				_loaded=true;
			}

			IEnumerable<SearchResultItem> _items;
			public IEnumerable<SearchResultItem> Items { get { if (!_loaded) Load(); return _items; } }

			int _count;
			public int Count { get { if (!_loaded) Load(); return _count; } }

			public void Dispose()
			{
				if (_items is IDisposable disp)
					disp.Dispose();

				if (_dbLock.CurrentCount==0)
					_dbLock.Release();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void ProcessHighlight(string hlVal,Tuple<string,string,string,int> hf,StringBuilder sb,Regex regex)
			{
				Match m=regex.Match(hlVal);
				if (m.Success)
				{
					sb.Append(hlVal);
					sb.Insert(m.Index+m.Length,hf.Item3);

					sb.Insert(m.Index,hf.Item2);
					if (m.Index>64)
						sb.Remove(0,m.Index-64);

					int overLen=sb.Length-128;
					if (overLen>0)
						sb.Remove(128,overLen);
				}
			}

			/*string _highlightQuery = @"\w";
			Regex hiLightRegex = new Regex(_highlightQuery);
			string sent = "dlouhá větička. a něco něcíčko.";
			string[] res = Enumerable.Range(0, 35).Select(x => TruncateSentence(sent, hiLightRegex, x)).ToArray();

			string TruncateSentence(string text, Regex regex, int maxLen)
			{
				if ((text == null) || (text.Length <= maxLen))
					return text;

				for (int a = maxLen; a >= 0; a--)
					if (!regex.IsMatch(text[a].ToString()))
					{
						maxLen = a;
						break;
					}

				return text.Substring(0, maxLen);
			}*/
		}

		class EmptySearchResult:ISearchResult
		{
			public IEnumerable<SearchResultItem> Items => Enumerable.Empty<SearchResultItem>();

			public int Count => 0;

			public void Dispose()
			{ }
		}
		#endregion SearchResult

		#region FacetResult
		class FacetResult:IFacetResult
		{
			PreparedCommand _preparedCommand;
			SqlConnection _dbConnection;
			bool _loaded;
			internal FacetResult(PreparedCommand preparedCommand,SqlConnection dbConnection)
			{
				_preparedCommand=preparedCommand;
				_dbConnection=new SqlConnection(dbConnection.ConnectionString);
			}

			void Load()
			{
				SqlCommand cmd = null;
				PeekableDataReader dr = null;
				try
				{
					_dbConnection.Open();
					cmd=CreateCommand(_dbConnection,_preparedCommand);

					dr=new PeekableDataReader(cmd.ExecuteReaderAsync().Result);
					_terms=new DataReaderEnumerable<FacetResultTermItem>(dr,new Action(() => { dr.Dispose(); cmd.Dispose(); _dbConnection.Dispose(); }),() =>
					new FacetResultTermItem()
					{
						Value=dr.GetValue(0),
						Count=dr.GetInt32(1)
					});
					_total=dr.Peek() ? dr.GetInt32(2) : 0;
					_missing=0;
					_other=0;
				}
				catch
				{
					if (dr!=null)
						dr.Dispose();
					if (cmd!=null)
						cmd.Dispose();
					_dbConnection.Dispose();
					throw;
				}

				_loaded=true;
			}

			int _missing;
			public int Missing { get { if (!_loaded) Load(); return _missing; } }

			int _total;
			public int Total { get { if (!_loaded) Load(); return _total; } }

			int _other;
			public int Other { get { if (!_loaded) Load(); return _other; } }

			IEnumerable<FacetResultTermItem> _terms;
			public IEnumerable<FacetResultTermItem> Terms { get { if (!_loaded) Load(); return _terms; } }

			public void Dispose()
			{
				if (_terms is IDisposable disp)
					disp.Dispose();
				_dbConnection.Dispose();
			}
		}
		#endregion FacetResult

		#region DeleteByQueryResult
		class DeleteByQueryResult:IDeleteByQueryResult
		{
			IEnumerable<DeleteResultItem> _deleteResults;
			internal DeleteByQueryResult(IEnumerable<DeleteResultItem> deleteResultItems)
			{
				_deleteResults=deleteResultItems;
			}

			public IEnumerable<DeleteResultItem> DeleteResults => _deleteResults;
		}
		#endregion DeleteByQueryResult

		#region CreateCommand
		static SqlCommand CreateCommand(SqlConnection dbConnection,PreparedCommand preparedCommand)
		{
			SqlCommand cmd = dbConnection.CreateCommand();
			cmd.CommandText=preparedCommand.CommandText;
			cmd.CommandTimeout=180000;

			foreach (KeyValuePair<string,object> item in preparedCommand.Parameters)
			{
				IDbDataParameter p = cmd.CreateParameter();
				p.ParameterName=item.Key;
				if (item.Value is DateTime)
					((SqlParameter)p).SqlDbType=SqlDbType.DateTime2;
				p.Value=item.Value;
				cmd.Parameters.Add(p);
			}

			return cmd;
		}
		#endregion CreateCommand

		#region CreateSearchQueryBuilder & CreateFacetQueryBuilder & CreateDeleteQueryBuilder
		public ISearchQueryBuilder CreateSearchQueryBuilder()
		{
			return new SearchQueryBuilder(_dbConnection);
		}

		public IFacetQueryBuilder CreateFacetQueryBuilder()
		{
			return new FacetQueryBuilder(_dbConnection);
		}

		public IDeleteByQueryBuilder CreateDeleteQueryBuilder()
		{
			return new DeleteByQueryBuilder(_dbConnection);
		}
		#endregion CreateSearchQueryBuilder & CreateFacetQueryBuilder & CreateDeleteQueryBuilder

		#region QueryBuilder
		internal const string TotalCountFieldName = "[!!totalCount!!]";

		class SearchQueryBase:IQueryBuilder
		{
			public void AddTerm(string fieldName,object value)
			{
				AddWhere($"(exists(select * from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName={this.AddParm(fieldName)} and d.{GetValueFieldNameByValue(value)}={AddParm(value)}))");
			}

			public void AddTerms(string fieldName,object[] values)
			{
				StringBuilder clause = new StringBuilder($"(exists(select * from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName={AddParm(fieldName)} and ");

				if (values.Length==0)
					clause.Append("0=1");
				else
				{
					clause.Append("d.").Append(GetValueFieldNameByValue(values[0]));
					if (values.Length==1)
						clause.Append("=").Append(AddParm(values[0]));
					else
						clause.Append(" in (").AppendJoin(",",values.Select(x => this.AddParm(x))).Append(")");
				}

				AddWhere(clause.Append("))").ToString());
			}

			public IQueryBuilder AddAnd()
			{
				return AddWhere(new AndQueryBuilder(this));
			}

			public IQueryBuilder AddOr()
			{
				return AddWhere(new OrQueryBuilder(this));
			}

			public IQueryBuilder AddNot()
			{
				return AddWhere(new NotQueryBuilder(this));
			}

			IQueryBuilder AddWhere(NestedQueryBuilder qb)
			{
				_whereParts.Add(qb);
				return qb;
			}

			public void AddRange(string fieldName,object valueFrom,bool inclusiveFrom,object valueTo,bool inclusiveTo)
			{
				bool propFromDefined = valueFrom!=null;
				bool propToDefined = valueTo!=null;

				string valFieldName = GetValueFieldNameByValue(propFromDefined ? valueFrom : valueTo);
				string[] clauseRoot = new[] { $"(exists(select * from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName={AddParm(fieldName)} and (",")))" };
				if (inclusiveFrom&&inclusiveTo)
					AddWhere($"{clauseRoot[0]}{valFieldName} between {AddParm(valueFrom)} and {AddParm(valueTo)}{clauseRoot[1]}");
				else
				{
					StringBuilder sb = new StringBuilder(clauseRoot[0]);
					if (propFromDefined)
					{
						sb.Append(valFieldName).Append(">");
						if (inclusiveFrom)
							sb.Append("=");
						sb.Append(AddParm(valueFrom));

						if (propToDefined)
							sb.Append(" and ");
					}
					if (propToDefined)
					{
						sb.Append(valFieldName).Append("<");
						if (inclusiveTo)
							sb.Append("=");
						sb.Append(AddParm(valueTo));
					}
					sb.Append(clauseRoot[1]);
					AddWhere(sb.ToString());
				}
			}

			public void AddExists(string fieldName)
			{
				AddWhere($"(exists(select * from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName={AddParm(fieldName)}))");
			}

			public void AddFullText(string searchWord,string[] inFields)
			{
				StringBuilder sb = new StringBuilder("(exists(select * from ContentIndex d where d.IdContent=c.IdContent and d.[Type]='ValueString' and d.ValueString like '%'+")
					 .Append(AddParm(searchWord)).Append("+'%'");
				if ((inFields!=null)&&(inFields.Length!=0))
				{
					sb.Append(" and PropertyName in (");
					bool addComma=false;
					foreach (string s in inFields)
					{
						if (addComma)
							sb.Append(",");
						addComma=true;
						sb.Append(AddParm(s));
					}
					sb.Append(")");
				}
				sb.Append("))");
				AddWhere(sb.ToString());
			}

			internal readonly List<Tuple<string,string>> _selectParts = new List<Tuple<string,string>>();
			internal readonly List<Tuple<string,string,string>> _tables = new List<Tuple<string,string,string>>();
			internal readonly protected List<SearchQueryBase> _whereParts = new List<SearchQueryBase>();
			internal readonly Dictionary<string,object> _parameters = new Dictionary<string,object>();

			internal virtual SearchQueryBase AddSelect(Tuple<string,string> column)
			{
				_selectParts.Add(column);
				return this;
			}

			internal virtual SearchQueryBase AddTable(string tableName,string alias,string join)
			{
				_tables.Add(new Tuple<string,string,string>(tableName,alias,join));
				return this;
			}

			internal virtual SearchQueryBase AddWhere(string condition)
			{
				AddWhere(new ConditionQueryBuilder(condition));
				return this;
			}

			internal virtual string AddParm(object value)
			{
				string parmName = "@p"+(_parameters.Count+1).ToString("00");
				_parameters.Add(parmName,value);
				return parmName;
			}

			internal virtual string GenerateQuery() => throw new NotImplementedException();
		}

		class SearchQueryBuilder:SearchQueryBase,ISearchQueryBuilder
		{
			SqlConnection _dbConnection;
			internal SearchQueryBuilder(SqlConnection dbConnection)
			{
				_dbConnection=dbConnection;
			}

			public Task<ISearchResult> ExecuteAsync()
			{
				if ((_take.HasValue)&&(_take.Value<1))
					return Task.FromResult((ISearchResult)new EmptySearchResult());

				string tempString;
				if (_sortFields==null)
					_sortFields=new ISortInfo[0];
				_requestedFields=_requestedFields==null?new string[0]:_requestedFields.Where(x => TryGetValueFieldNameByName(x, out tempString)).ToArray();
				if (_scriptFields==null)
					_scriptFields=new ScriptField[0];
				if (_highlightFields==null)
					_highlightFields=new HighlightFieldRequest[0];
				
				this
					 .AddSelect(new Tuple<string,string>("IdContent",null))
					 .AddSelect(new Tuple<string,string>("ValueString","LanguageID"))
					 .AddSelect(new Tuple<string,string>("count(*) over()",SqlIndexRepository.TotalCountFieldName))
					 .AddSelect(new Tuple<string,string>("(select d.ValueString from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName='!!Index.Id')","[!!Index.Id!!]"))
					 .AddSelect(new Tuple<string,string>("(select d.ValueString from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName='!!Index.Name')","[!!Index.Name!!]"))
					 .AddSelect(new Tuple<string,string>("(select d.ValueString from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName='!!Index.Type')","[!!Index.Type!!]"))
					 .AddSelect(new Tuple<string,string>("(select replace(stuff((select '@'+d.ValueString from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName='___types' for xml path ('')),1,1,''),'@',char(13)))","[!!Index.Types!!]"))
					 .AddTable("ContentIndex","c",null)
					 .AddWhere("PropertyName='LanguageID$$string'");
				foreach (string fieldName in _requestedFields)
					this.AddSelect(new Tuple<string,string>($"(select d.{GetValueFieldNameByName(fieldName)} from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName={AddParm(NormalizeValueFieldName(fieldName))})",$"[{fieldName}]"));
				foreach (ScriptField sf in _scriptFields)
				{
					if (sf.Script=="ascropped")
					{
						string fieldName=sf.Param("field");
						try
						{
							this.AddSelect(new Tuple<string,string>($"(select substring(d.{GetValueFieldNameByName(fieldName)},1,{sf.Param("length")}) from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName={AddParm(NormalizeValueFieldName(fieldName))})", $"[{sf.Name}]"));
						}
						catch //provide empty string for unsupported data types (when GetValueFieldNameByName throws an exception)
						{
							this.AddSelect(new Tuple<string,string>($"('')", $"[{sf.Name}]"));
						}
					}
					else
						throw new Exception("Unsupported script");
				}
				Tuple<string,string,string>[] hfs=_highlightFields.Select(hf=>
				{
					string fieldName=hf.FieldName;
					try
					{
						this.AddSelect(new Tuple<string,string>($"(select d.{GetValueFieldNameByName(fieldName)} from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName={AddParm(NormalizeValueFieldName(fieldName))})",$"[{GetHiLightFieldName(fieldName)}]"));
						return new Tuple<string,string,string>(hf.FieldName,string.Concat(hf.PreTags??new string[0]),string.Concat(hf.PostTags??new string[0]));
					}
					catch
					{
						return null;
					}
				}).Where(x=>x!=null).ToArray();

				string query=GenerateQuery();

				if (_skip.HasValue||_take.HasValue||_sortFields!=null)
				{
					List<Tuple<string,string,bool>> sortFields=new List<Tuple<string,string,bool>>();
					if (_sortFields.Length==0)
						sortFields.Add(new Tuple<string,string,bool>("0 [!!sort!!]","[!!sort!!]",false));
					else
					{
						int ind = 0;
						sortFields.AddRange(_sortFields.Select(x =>
						{
							string sortFieldName = string.Concat("[!!sort",(ind++).ToString("00"),"!!]");
							return new Tuple<string,string,bool>($"(select d.{GetValueFieldNameByName(x.PropertyName)} from ContentIndex d where d.IdContent=qIn.IdContent and d.PropertyName={AddParm(x.PropertyName)}) {sortFieldName}",sortFieldName,x.Descendant);
						}));
					}

					StringBuilder sb = new StringBuilder();
					sb.Append("select ").AppendJoin(",",_selectParts.Select(x => x.Item2??x.Item1)).Append(" from (select distinct *,").AppendJoin(",",sortFields.Select(x => x.Item1)).Append(" from (")
						 .Append(query)
						 .Append(") qIn").Append(") q").Append(" order by ").AppendJoin(",",sortFields,x => { sb.Append(x.Item2); if (x.Item3) sb.Append(" desc"); });

					if ((_take.HasValue)&&(!_skip.HasValue))
						_skip=0;
					if (_skip.HasValue)
						sb.Append(" offset ").Append(_skip.Value).Append(" rows");
					if (_take.HasValue)
						sb.Append(" fetch next ").Append(_take.Value).Append(" rows only");

					query=sb.ToString();
				}

				return Task.FromResult((ISearchResult)new SearchResult(new PreparedCommand { CommandText=query,Parameters=_parameters.ToDictionary(x=>x.Key,x=>x.Value) },_dbConnection,_requestedFields.Concat(_scriptFields.Select(x=>x.Name)).ToArray(),hfs,_highlightQuery));
			}

			ISortInfo[] _sortFields;
			public void AddSort(ISortInfo[] sort)
			{
				_sortFields=sort;
			}

			long? _take;
			public void SetResultSize(long? resultSize)
			{
				_take=resultSize;
			}

			long? _skip;
			public void SetSkipSize(long? skipSize)
			{
				_skip=skipSize;
			}

			string[] _requestedFields;
			public void SetRequestedFields(IEnumerable<string> fields)
			{
				//_requestedFields=fields.Where(x=>x!="___types"&&x!="$type"&&x!="Id$$string"&&x!="Language.Name$$string"&&!x.EndsWith("$$geo")).ToArray();
				_requestedFields=fields.Where(x=>
					!string.Equals(x,"___types",StringComparison.Ordinal)
					&&!string.Equals(x,"$type",StringComparison.Ordinal)
					&&!string.Equals(x,"Id$$string",StringComparison.Ordinal)
					&&!string.Equals(x,"Language.Name$$string",StringComparison.Ordinal)
					&&!x.EndsWith("$$geo",StringComparison.Ordinal)
					).ToArray();
			}

			ScriptField[] _scriptFields;
			public void SetScriptFields(IEnumerable<ScriptField> fields)
			{
				_scriptFields=fields.ToArray();
			}

			string _highlightQuery;
			HighlightFieldRequest[] _highlightFields;
			public void SetHighlights(string query,IEnumerable<HighlightFieldRequest> fields)
			{
				_highlightQuery=query;
				_highlightFields=fields.ToArray();
			}

			internal override string GenerateQuery()
			{
				return GenerateSelectQuery(_selectParts,_tables,_whereParts);
			}

			internal static string GenerateSelectQuery(IEnumerable<Tuple<string,string>> selectParts,IEnumerable<Tuple<string,string,string>> tables,IEnumerable<SearchQueryBase> whereParts)
			{
				StringBuilder sb = new StringBuilder("select ");
				int a = 0;
				foreach (Tuple<string,string> item in selectParts)
				{
					if (a++!=0)
						sb.Append(",");
					sb.Append(item.Item1);
					if (item.Item2!=null)
						sb.Append(' ').Append(item.Item2);
				}

				bool first = true;
				foreach (Tuple<string,string,string> item in tables)
				{
					if (first)
					{
						sb.AppendFormat(" from");
						first=false;
					}

					if (item.Item3!=null)
						sb.Append(" join");
					sb.Append(' ').Append(item.Item1);
					if (item.Item2!=null)
						sb.Append(' ').Append(item.Item2);
					if (item.Item3!=null)
						sb.Append(" on ").Append(item.Item3);
				}

				first = true;
				foreach (SearchQueryBase item in whereParts)
				{
					if (first)
					{
						sb.Append(" where ");
						first=false;
					}
					else
						sb.Append(" and ");
					sb.Append(item.GenerateQuery());
				}

				return sb.ToString();
			}

			internal static string GetHiLightFieldName(string fieldName)
				=> $"!!HiLi.{fieldName}!!";
		}

		class FacetQueryBuilder:SearchQueryBase, IFacetQueryBuilder
		{
			SqlConnection _dbConnection;
			internal FacetQueryBuilder(SqlConnection dbConnection)
			{
				_dbConnection=dbConnection;
			}

			string _fieldName;
			public void SetField(string fieldName)
			{
				_fieldName=fieldName;
			}

			int? _resultSize;
			public void SetResultSize(int? resultSize)
			{
				_resultSize=resultSize;
			}

			public Task<IFacetResult> ExecuteAsync()
			{
				if (_fieldName==null)
					throw new InvalidOperationException($"Field must be set by {nameof(SetField)} method");

				string valueFieldName = GetValueFieldNameByName(_fieldName);
				StringBuilder query = new StringBuilder().Append("select ");
				if (_resultSize.HasValue)
					query.Append("top ").Append(_resultSize.Value).Append(" ");
				query.Append(valueFieldName).Append(",count(*),count(*) over() ").Append(TotalCountFieldName)
					 .Append(" from ContentIndex where PropertyName=@p group by ").Append(valueFieldName);
				PreparedCommand preparedCommand = new PreparedCommand { CommandText=query.ToString(),Parameters=new Dictionary<string,object> { { "@p",_fieldName } } };
				return Task.FromResult((IFacetResult)new FacetResult(preparedCommand,_dbConnection));
			}
		}

		class DeleteByQueryBuilder:SearchQueryBase, IDeleteByQueryBuilder
		{
			SqlConnection _dbConnection;
			internal DeleteByQueryBuilder(SqlConnection dbConnection)
			{
				_dbConnection=dbConnection;
			}

			public async Task<IDeleteByQueryResult> ExecuteAsync()
			{
				this
					.AddSelect(new Tuple<string,string>("distinct c.ValueString","IndexName"))
					.AddSelect(new Tuple<string,string>("c.IdContent","Id"))
					.AddTable("ContentIndex","c",null)
					.AddWhere("c.PropertyName='!!Index.Name'");

				string query = GenerateQuery();
				query=string.Concat("select * into #temp from (",query,") a;\r\ndelete from c from ContentIndex c join #temp t on t.Id=c.IdContent;\r\nselect IndexName,count(*) CountInIndex from #temp group by IndexName");

				_dbLock.WaitAsync().Wait();
				SqlCommand cmd = null;
				SqlTransaction trans = null;
				SqlDataReader dr = null;
				List<DeleteResultItem> delRes = new List<DeleteResultItem>();
				try
				{
					cmd=CreateCommand(_dbConnection,new PreparedCommand { CommandText=query,Parameters=_parameters.ToDictionary(x => x.Key,x => x.Value) });
					cmd.Transaction=trans=_dbConnection.BeginTransaction();
					dr=await cmd.ExecuteReaderAsync();
					while (await dr.ReadAsync())
						delRes.Add(new DeleteResultItem { IndexName=dr.GetString(0),DeletedCount=dr.GetInt32(1) });
					dr.Dispose();
					dr=null;
					trans.Commit();
					trans=null;
				}
				finally
				{
					if (dr!=null)
						dr.Dispose();
					if (cmd!=null)
					{
						if (trans!=null)
							trans.Rollback();
						cmd.Dispose();
					}
					_dbLock.Release();
				}

				return new DeleteByQueryResult(delRes.ToArray());
			}

			internal override string GenerateQuery()
			{
				return SearchQueryBuilder.GenerateSelectQuery(_selectParts,_tables,_whereParts);
			}
		}

		abstract class NestedQueryBuilder:SearchQueryBase
		{
			protected SearchQueryBase _parentBuilder;
			internal NestedQueryBuilder(SearchQueryBase parentBuilder)
			{
				_parentBuilder=parentBuilder;
			}

			internal override string AddParm(object value)
			{
				return _parentBuilder.AddParm(value);
			}
		}

		class ConditionQueryBuilder:NestedQueryBuilder
		{
			string _condition;
			public ConditionQueryBuilder(string condition) : base(null)
			{
				_condition=condition;
			}

			internal override string GenerateQuery()
			{
				return _condition;
			}
		}

		abstract class AndOrQueryBuilder:NestedQueryBuilder
		{
			public AndOrQueryBuilder(SearchQueryBase parentBuilder) : base(parentBuilder)
			{ }

			internal override string GenerateQuery()
			{
				return _whereParts.Count==0
			  ? ""
				: new StringBuilder("(").AppendJoin(string.Concat(" ",GetOperand()," "),_whereParts.Select(x => x.GenerateQuery()))
						  .Append(')').ToString();
			}

			protected abstract string GetOperand();
		}

		class AndQueryBuilder:AndOrQueryBuilder
		{
			internal AndQueryBuilder(SearchQueryBase parentBuilder) : base(parentBuilder)
			{ }

			protected override string GetOperand()
			{
				return "and";
			}
		}

		class OrQueryBuilder:AndOrQueryBuilder
		{
			internal OrQueryBuilder(SearchQueryBase parentBuilder) : base(parentBuilder)
			{ }

			protected override string GetOperand()
			{
				return "or";
			}
		}

		class NotQueryBuilder:NestedQueryBuilder
		{
			internal NotQueryBuilder(SearchQueryBase parentBuilder) : base(parentBuilder)
			{ }

			internal override string GenerateQuery()
			{
				return string.Concat("(not (",_whereParts[0].GenerateQuery(),"))");
			}
		}
		#endregion QueryBuilder

		#region PreparedCommand
		class PreparedCommand
		{
			public string CommandText { get; internal set; }

			public IDictionary<string,object> Parameters { get; internal set; }
		}
		#endregion PreparedCommand

		#region DebugFormat
#if DEBUG
		//https://www.freeformatter.com/sql-formatter.html
		static string DebugFormat(IDbCommand cmd)
		{
			/*StringBuilder sb = new StringBuilder();
			foreach (SqlParameter parm in cmd.Parameters)
			{
				sb.Append(sb.Length == 0 ? "declare " : ",")
					.Append(parm.ParameterName).Append(' ').Append(parm.SqlDbType);
				if (parm.SqlDbType == SqlDbType.NVarChar)
					sb.Append('(').Append(parm.Size).Append(')');

				sb.Append('=');

				object val = parm.Value;
				if ((val == null) || (val == DBNull.Value))
					val = "null";
				else if (val is string strVal)
					val = $"'{strVal}'";
				else if (val is DateTime dtVal)
					val = $"'{dtVal.ToString("s", System.Globalization.CultureInfo.InvariantCulture).Replace('T', ' ')}'";
				else if (val is bool boolVal)
					val = boolVal ? "1" : "0";
				sb.Append(val.ToString());
			}

			sb.AppendLine().AppendLine(cmd.CommandText);
			return sb.ToString();*/

			StringBuilder sb = new StringBuilder(cmd.CommandText);

			foreach (SqlParameter parm in cmd.Parameters.OfType<SqlParameter>().OrderByDescending(x=>x.ParameterName.Length))
			{
				int pos = SbIndexOf(sb, parm.ParameterName, 0, false);

				object val = parm.Value;
				if ((val == null) || (val == DBNull.Value))
					val = "null";
				else if (val is string strVal)
					val = $"'{strVal}'";
				else if (val is DateTime dtVal)
					val = $"'{dtVal.ToString("s", System.Globalization.CultureInfo.InvariantCulture).Replace('T', ' ')}'";
				else if (val is bool boolVal)
					val = boolVal ? "1" : "0";

				sb.Remove(pos, parm.ParameterName.Length).Insert(pos, val.ToString());
			}

			return sb.ToString();
		}

		/// <summary>
		/// Returns the index of the start of the contents in a StringBuilder
		/// </summary>        
		/// <param name="value">The string to find</param>
		/// <param name="startIndex">The starting index.</param>
		/// <param name="ignoreCase">if set to <c>true</c> it will ignore case</param>
		/// <returns></returns>
		static int SbIndexOf(StringBuilder sb, string value, int startIndex, bool ignoreCase)
		{
			int index;
			int length = value.Length;
			int maxSearchLength = (sb.Length - length) + 1;

			if (ignoreCase)
			{
				for (int i = startIndex; i < maxSearchLength; ++i)
				{
					if (Char.ToLower(sb[i]) == Char.ToLower(value[0]))
					{
						index = 1;
						while ((index < length) && (Char.ToLower(sb[i + index]) == Char.ToLower(value[index])))
							++index;

						if (index == length)
							return i;
					}
				}

				return -1;
			}

			for (int i = startIndex; i < maxSearchLength; ++i)
			{
				if (sb[i] == value[0])
				{
					index = 1;
					while ((index < length) && (sb[i + index] == value[index]))
						++index;

					if (index == length)
						return i;
				}
			}

			return -1;
		}
#endif
		#endregion DebugFormat
	}

	#region extensions
	internal static class StringBuilderExtensions
	{
		internal static StringBuilder AppendJoin(this StringBuilder sb,string delimiter,IEnumerable<string> values)
		{
			bool useDel = false;
			foreach (string val in values)
			{
				if (useDel)
					sb.Append(delimiter);
				useDel=true;
				sb.Append(val);
			}
			return sb;
		}

		internal static StringBuilder AppendJoin<T>(this StringBuilder sb,string delimiter,IEnumerable<T> values,Action<T> appendAction)
		{
			bool useDel = false;
			foreach (T val in values)
			{
				if (useDel)
					sb.Append(delimiter);
				useDel=true;
				appendAction(val);
			}
			return sb;
		}
	}

	internal static class ScriptFieldExtensions
	{
		internal static string Param(this ScriptField scriptField, string name)
		{
			if ((scriptField.Parameters.TryGetValue(name,out object result))&&(result!=null))
				return result.ToString();
			throw new Exception($"Script field does not contain {name} parameter value.");
		}
	}
	#endregion extensions
}
