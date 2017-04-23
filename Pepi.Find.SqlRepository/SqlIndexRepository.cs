#region using
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
		public async Task SaveIndexItemsAsync(List<IndexItem> indexItems)
		{
			foreach (IndexItem item in indexItems)
			{
				SqlTransaction trans = null;
				await _dbLock.WaitAsync();
				try
				{
					trans=_dbConnection.BeginTransaction();

					long contentId = (long)item.Content.Values.Find(x => x.Key=="ContentLink.ID$$number").Value;

					using (IDbCommand cmd = _dbConnection.CreateCommand())
					{
						cmd.Transaction=trans;
						cmd.CommandText=$"delete ContentIndex where IdContent=@IdContent";
						AddParm(cmd,"@IdContent",contentId);
						cmd.ExecuteNonQuery();
					}

					IEnumerable<KeyValuePair<string,object>> valuesToSave = (new[] { new KeyValuePair<string,object>("!!Index.Id",item.Index.Id),new KeyValuePair<string,object>("!!Index.Name",item.Index.Name),new KeyValuePair<string,object>("!!Index.Type",item.Index.Type) })
						 .Concat(item.Content.Values);
					foreach (KeyValuePair<string,object> value in valuesToSave)
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
						}
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

		#region WriteErrToDb
		public void WriteErrToDb(Exception ex,string request)
		{
			ex=ex.GetBaseException();
			_dbLock.Wait();
			try
			{
				using (IDbCommand cmd = _dbConnection.CreateCommand())
				{
					cmd.CommandText="insert Error ([TimeStamp],[Message],StaskTrace,Request) values (@TimeStamp,@Message,@StaskTrace,@Request)";
					AddParm(cmd,"@TimeStamp",DateTime.Now);
					AddParm(cmd,"@Message",ex.Message);
					AddParm(cmd,"@StaskTrace",ex.StackTrace??"");
					AddParm(cmd,"@Request",request);
					cmd.ExecuteNonQuery();
				}
			}
			finally
			{
				_dbLock.Release();
			}
		}
		#endregion WriteErrToDb

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
			Type valType = value.GetType();
			if (valType==typeof(string))
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
			int pos = fieldName.IndexOf("$$");
			if (pos==-1)
				throw new Exception("Undetermined property type");
			fieldName=fieldName.Substring(pos+2);

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
			throw new Exception("Unsupported property type");
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
			public bool MoveNext() { _valTaken=false; bool res = _dr.Read(); return res; }
			public void Reset() => throw new NotImplementedException();
		}
		#endregion DataReaderEnumerator

		#region SearchResult
		class SearchResult:ISearchResult
		{
			PreparedCommand _preparedCommand;
			SqlConnection _dbConnection;
			bool _loaded;
			internal SearchResult(PreparedCommand preparedCommand,SqlConnection dbConnection)
			{
				_preparedCommand=preparedCommand;
				_dbConnection=dbConnection;
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
					_items=new DataReaderEnumerable<SearchResultItem>(dr,new Action(() => { dr.Dispose(); cmd.Dispose(); if (_dbLock.CurrentCount==0) _dbLock.Release(); }),() =>
					{
						SearchResultItem sri = new SearchResultItem();
						sri.Index.Name=dr.IsDBNull(4) ? "" : dr.GetString(4);
						sri.Index.ContentTypeName=dr.IsDBNull(5) ? "" : dr.GetString(5);
						sri.Index.Id=dr.IsDBNull(3) ? "" : dr.GetString(3);
						sri.Document.Id=dr.GetInt32(0);
						sri.Document.LanguageName=dr.GetString(1);
						return sri;
					});
					_count=dr.Peek() ? dr.GetInt32(2) : 0;
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

		#region CreateSearchQueryBuilder & CreateFacetQueryBuilder
		public ISearchQueryBuilder CreateSearchQueryBuilder()
		{
			return new SearchQueryBuilder(_dbConnection);
		}

		public IFacetQueryBuilder CreateFacetQueryBuilder()
		{
			return new FacetQueryBuilder(_dbConnection);
		}
		#endregion CreateSearchQueryBuilder & CreateFacetQueryBuilder

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
					bool addComma = false;
					foreach (string s in inFields)
					{
						if (!addComma)
							sb.Append(",");
						addComma=false;
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

		class SearchQueryBuilder:SearchQueryBase, ISearchQueryBuilder
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

				this
					 .AddSelect(new Tuple<string,string>("IdContent",null))
					 .AddSelect(new Tuple<string,string>("ValueString","LanguageID"))
					 .AddSelect(new Tuple<string,string>("count(*) over()",SqlIndexRepository.TotalCountFieldName))
					 .AddSelect(new Tuple<string,string>("(select d.ValueString from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName='!!Index.Id')","[!!Index.Id!!]"))
					 .AddSelect(new Tuple<string,string>("(select d.ValueString from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName='!!Index.Name')","[!!Index.Name!!]"))
					 .AddSelect(new Tuple<string,string>("(select d.ValueString from ContentIndex d where d.IdContent=c.IdContent and d.PropertyName='!!Index.Type')","[!!Index.Type!!]"))
					 .AddTable("ContentIndex","c",null)
					 .AddWhere("PropertyName='LanguageID$$string'");

				string query = GenerateQuery();

				if (_skip.HasValue||_take.HasValue||_sortFields!=null)
				{
					List<Tuple<string,string,bool>> sortFields = new List<Tuple<string,string,bool>>();
					if (_sortFields==null)
						sortFields.Add(new Tuple<string,string,bool>("0 [!!sort!!]","[!!sort!!]",false));
					else
					{
						int ind = 0;
						sortFields.AddRange(_sortFields.Select(x =>
						{
							string sortFieldName = string.Concat("[!!sort",(ind++).ToString("00"),"!!]");
							return new Tuple<string,string,bool>($"(select d.{GetValueFieldNameByName(x.PropertyName)} from ContentIndex d where d.IdContent=qIn.IdContent and d.PropertyName={ AddParm(x.PropertyName)}) {sortFieldName}",sortFieldName,x.Descendant);
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

				return Task.FromResult((ISearchResult)new SearchResult(new PreparedCommand { CommandText=query,Parameters=_parameters.ToDictionary(x => x.Key,x => x.Value) },_dbConnection));
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

			internal override string GenerateQuery()
			{
				StringBuilder sb = new StringBuilder("select ");
				int a = 0;
				foreach (Tuple<string,string> item in _selectParts)
				{
					if (a++!=0)
						sb.Append(",");
					sb.Append(item.Item1);
					if (item.Item2!=null)
						sb.Append(' ').Append(item.Item2);
				}
				if (_tables.Count!=0)
				{
					sb.AppendFormat(" from");
					foreach (Tuple<string,string,string> item in _tables)
					{
						if (item.Item3!=null)
							sb.Append(" join");
						sb.Append(' ').Append(item.Item1);
						if (item.Item2!=null)
							sb.Append(' ').Append(item.Item2);
						if (item.Item3!=null)
							sb.Append(" on ").Append(item.Item3);
					}
				}
				if (_whereParts.Count!=0)
				{
					bool first = true;
					foreach (SearchQueryBase item in _whereParts)
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
				}

				return sb.ToString();
			}
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
	#endregion extensions
}
