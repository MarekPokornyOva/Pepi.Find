#region using
using EPiServer.Find;
using Pepi.Find.Server.Abstract;
using System;
using System.Reflection;
//using EPiServer.Find.Framework;
#endregion using

namespace Pepi.Find.Direct
{
	public static class SearchClientExtensions
	{
		/*public static void SetDefaultClient(IClient client)
		{
			typeof(SearchClient).GetField("instance",BindingFlags.NonPublic|BindingFlags.Static)
				.SetValue(null,client);
		}

		public static void SetDefaultDirectClientCms(IIndexRepository indexRepository)
		{
			//SetDefaultClient(new DirectClientCms(indexRepository));

			IClientConventions conv = EPiServer.Find.Framework.SearchClient.Instance.Conventions;
			Pepi.Find.Direct.SearchClientExtensions.SetDefaultClient(new Pepi.Find.Direct.DirectClient(new Pepi.Find.SqlRepository.SqlIndexRepository(@"Data Source=WL304094\MSSQLSERVER2014;Initial Catalog=TietoComFindIndex;Integrated Security=True;Connection Timeout=60;MultipleActiveResultSets=True")));
			EPiServer.Find.Framework.SearchClient.Instance.Conventions = conv;
		}*/

		static SearchClientExtensions()
		{
			_searchClientInstance = Type.GetType("EPiServer.Find.Framework.SearchClient, EPiServer.Find.Framework")
				.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
		}

		static FieldInfo _searchClientInstance;
		static IClient ActiveClient
		{
			get => (IClient)_searchClientInstance.GetValue(null);
			set => _searchClientInstance.SetValue(null, value);
		}

		public static void SetDefaultClient(IClient client)
		{
			ActiveClient = client;
		}

		public static void SetDefaultDirectClient(IIndexRepository indexRepository)
		{
			IClientConventions conv = ActiveClient.Conventions;
			SetDefaultClient(new DirectClient(indexRepository) { Conventions = conv });
		}
	}
}
