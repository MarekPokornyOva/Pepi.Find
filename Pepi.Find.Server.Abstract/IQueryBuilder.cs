namespace Pepi.Find.Server.Abstract
{
	public interface IQueryBuilder
	{
		IQueryBuilder AddAnd();
		IQueryBuilder AddOr();
		IQueryBuilder AddNot();

		void AddTerm(string fieldName,object value);
		void AddTerms(string fieldName,object[] values);
		void AddRange(string fieldName,object valueFrom,bool inclusiveFrom,object valueTo,bool inclusiveTo);
		void AddExists(string fieldName);
		void AddFullText(string searchWord,string[] inFields);
	}
}
