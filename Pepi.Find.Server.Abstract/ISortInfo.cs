namespace Pepi.Find.Server.Abstract
{
	public interface ISortInfo
	{
		string PropertyName { get; }
		bool Descendant { get; }
	}
}
