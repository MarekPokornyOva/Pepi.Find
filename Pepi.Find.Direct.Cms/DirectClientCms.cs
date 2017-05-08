#region using
using EPiServer.Find.Cms;
using Pepi.Find.Server.Abstract;
#endregion using

namespace Pepi.Find.Direct.Cms
{
	public class DirectClientCms:DirectClient
	{
		public DirectClientCms(IIndexRepository indexRepository) : base(indexRepository)
		{
			CmsClientConventions.ApplyCmsConventions(this);
		}
	}
}
