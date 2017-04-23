namespace Pepi.Find.Server.Abstract
{
	public interface IHandlerContext
	{
		IHandlerRequest Request { get; }
		IHandlerResponse Response { get; }
	}
}
