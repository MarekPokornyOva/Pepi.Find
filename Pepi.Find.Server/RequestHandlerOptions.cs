namespace Pepi.Find.Server
{
	public class RequestHandlerOptions
	{
		public int? SearchMaxCount { get; set; }
		public int? SearchDefaultCount { get; set; }
		public int? FacetDefaultCount { get; set; }

		public static RequestHandlerOptions Compatible => new RequestHandlerOptions { SearchMaxCount=1000,SearchDefaultCount=10,FacetDefaultCount=10 };
		public static RequestHandlerOptions Unlimited => new RequestHandlerOptions();
	}
}
