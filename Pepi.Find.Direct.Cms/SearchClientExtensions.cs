#region using
using EPiServer.Find;
using Pepi.Find.Server.Abstract;
using System.Reflection;
using EPiServer.Find.Framework;
using Pepi.Find.Direct.Cms;
#endregion using

namespace Pepi.Find.Direct
{
	public static class SearchClientExtensions
	{
		public static void SetDefaultClient(IClient client)
		{
			typeof(SearchClient).GetField("instance",BindingFlags.NonPublic|BindingFlags.Static)
				.SetValue(null,client);
		}

		public static void SetDefaultDirectClientCms(IIndexRepository indexRepository)
		{
			SetDefaultClient(new DirectClientCms(indexRepository));
		}
	}
}
