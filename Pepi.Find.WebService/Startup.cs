#region using
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pepi.Find.Server.Abstract;
using Pepi.Find.Server;
using Pepi.Find.SqlRepository;
using Pepi.Find.WebService.Util;
#endregion using

namespace Pepi.Find.WebService
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			//services.AddSingleton<IIndexRepository>(sp=>new SqlIndexRepository());
			services.AddSingleton<IIndexRepository,NullIndexRepository>();
			services.AddSingleton<IRequestHandler,DefaultRequestHandler>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app,IHostingEnvironment env,ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole();

			if (env.IsDevelopment())
				app.UseDeveloperExceptionPage();

			app.Run(async (context) =>
			{
				await context.RequestServices.GetRequiredService<IRequestHandler>()
					.ProcessRequestAsync(new WebContext(context));
			});
		}
	}
}
