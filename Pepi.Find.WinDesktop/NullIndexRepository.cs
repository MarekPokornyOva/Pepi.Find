#region using
using Pepi.Find.Server.Abstract;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion using

namespace Pepi.Find.WinDesktop
{
	class NullIndexRepository:IIndexRepository
	{
		public IFacetQueryBuilder CreateFacetQueryBuilder() => new FakeFacetQueryBuilder();

		public ISearchQueryBuilder CreateSearchQueryBuilder() => new FakeSearchQueryBuilder();

		public Task SaveIndexItemsAsync(List<IndexItem> indexItems) => Task.CompletedTask;

		class FakeFacetQueryBuilder:IFacetQueryBuilder
		{
			public Task<IFacetResult> ExecuteAsync() => Task.FromResult((IFacetResult)new FakeFacetResult());

			public void SetField(string fieldName) { }

			public void SetResultSize(int? resultSize) { }

			class FakeFacetResult:IFacetResult
			{
				public int Missing => 0;

				public int Total => 0;

				public int Other => 0;

				public IEnumerable<FacetResultTermItem> Terms => Enumerable.Empty<FacetResultTermItem>();

				public void Dispose() { }
			}
		}

		class FakeSearchQueryBuilder:ISearchQueryBuilder
		{
			public IQueryBuilder AddAnd() => this;

			public void AddExists(string fieldName) { }

			public void AddFullText(string searchWord,string[] inFields) { }

			public IQueryBuilder AddNot() => this;

			public IQueryBuilder AddOr() => this;

			public void AddRange(string fieldName,object valueFrom,bool inclusiveFrom,object valueTo,bool inclusiveTo) { }

			public void AddSort(ISortInfo[] sort) { }

			public void AddTerm(string fieldName,object value) { }

			public void AddTerms(string fieldName,object[] values) { }

			public Task<ISearchResult> ExecuteAsync() => Task.FromResult((ISearchResult)new FakeSearchResult());

			public void SetResultSize(long? resultSize) { }

			public void SetSkipSize(long? skipSize) { }

			class FakeSearchResult:ISearchResult
			{
				public IEnumerable<SearchResultItem> Items => Enumerable.Empty<SearchResultItem>();

				public int Count => 0;

				public void Dispose() { }
			}
		}
	}
}
