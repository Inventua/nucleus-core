using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using Nucleus.Core;
using Nucleus.Core.Layout;
using Nucleus.Core.Plugins;
using Nucleus.Core.DataProviders.SQLite;
using Nucleus.Core.Logging;
using Nucleus.Core.FileProviders;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Core.EventHandlers;
using Nucleus.Core.Authentication;
using Nucleus.Core.Authorization;

namespace Nucleus.Web
{
	public class Startup
	{
		private const string CONFIG_FILENAME = "appSettings.json";

		private IConfiguration Configuration { get; }
		private IWebHostEnvironment Environment { get; }

		public Startup(IConfiguration configuration, IWebHostEnvironment env)
		{
			this.Environment = env;

			this.Configuration = BuildConfiguration(configuration);
		}

		private IConfiguration BuildConfiguration(IConfiguration configuration)
		{
			ConfigurationBuilder config = new();
			string appConfigFile = $"{System.IO.Path.GetFileNameWithoutExtension(CONFIG_FILENAME)}.{this.Environment.EnvironmentName}{System.IO.Path.GetExtension(CONFIG_FILENAME)}";
						
			config
				.AddConfiguration(configuration)
				.AddJsonFile(System.IO.Path.Combine(ConfigFolder(), CONFIG_FILENAME), optional: true, reloadOnChange: true)
				.AddJsonFile(System.IO.Path.Combine(ConfigFolder(), appConfigFile), optional: true, reloadOnChange: true)
				.AddEnvironmentVariables();

			// Add all of the other .json files so that users can split their configuration however they like
			foreach (string configFile in System.IO.Directory.EnumerateFiles(ConfigFolder(), "*.json"))
			{
				string filename = System.IO.Path.GetFileName(configFile);
				if (!filename.Equals(CONFIG_FILENAME, StringComparison.OrdinalIgnoreCase) && !filename.Equals(appConfigFile, StringComparison.OrdinalIgnoreCase))
				{
					config.AddJsonFile(configFile, optional: true, reloadOnChange: true);
				}
			}

			return config.Build();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{			
			services.AddStartupLogger(this.Configuration);

			services.Logger().LogInformation(new[]
			{
				"",
				$"{System.Reflection.Assembly.GetExecutingAssembly().Product()} version {System.Reflection.Assembly.GetExecutingAssembly().Version()}. {System.Reflection.Assembly.GetExecutingAssembly().Copyright()}",
				$"Application Root folder: [{System.Environment.CurrentDirectory}]",
				$"Configuration folder:    [{ConfigFolder()}]",
				$"Data folder:             [{Folders.GetDataFolder()}]",
				$"Content Root:            [{this.Environment.ContentRootPath}]",
				$"Web Root:                [{this.Environment.WebRootPath}]",
				$"Environment:             [{this.Environment.EnvironmentName}]",
				""
			});				

			IMvcBuilder builder = services.AddControllersWithViews();

			builder.AddRazorRuntimeCompilation();

			//			services.AddLocalization(options => options.ResourcesPath = "LocalizationResources");

			// Enable logging
			services.AddLogging(logging =>
			{
				logging.ClearProviders();
				logging.AddDebugLogger();
				logging.AddTextFileLogger(this.Configuration);
			});

			// Enable compression
			// TODO: Make this configurable
			services.Logger().LogInformation($"Adding Response Compression");
			services.AddResponseCompression(options =>
			{
				options.Providers.Add(typeof(Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider));
				options.Providers.Add(typeof(Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider));
				options.EnableForHttps = true;
			});

			// TODO: Make this configurable
			services.Configure<ForwardedHeadersOptions>(options =>
			{
				options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
			});

			builder.AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver());
			builder.SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Latest);

			// Read HostOptions settings from config
			services.Configure<HostOptions>(Configuration.GetSection("HostOptions"));

			// Add merged file provider.  
			services.AddMergedFileProvider(this.Configuration);

			// This makes embedded static files in our precompiled Razor assemblies [modules] work
			services.AddRazorEmbeddedFileProviders();

			// File minifier wraps all static file requests, thus must be the LAST file provider added.  
			services.AddMinifiedFileProvider(this.Configuration);

			// This could be replaced with .AddSQLServerDataProvider (etc), if we implement alternative data providers
			services.Logger().LogInformation($"Adding SQLite data provider");
			services.AddSQLiteDataProvider();

			//services.AddNucleusLayout();

			services.Logger().LogInformation($"Adding Nucleus core services");
			services.AddNucleusCoreServices(this.Configuration);
			services.AddScoped<IContainerController, Nucleus.Web.Controllers.ContainerController>();

			// moved to AddNucleusCoreServices
			//services.AddCoreEventHandlers();
			//services.AddFileSystemProviders(this.Configuration);

			builder.AddPlugins(Environment.ContentRootPath);

			services.AddRazorPages();

			services.AddCoreAuthentication(this.Configuration);
			services.AddCoreAuthorization();
			
			//// https://github.com/dotnet/AspNetCore.Docs/issues/6032
			//// https://stackoverflow.com/questions/57234141/iauthorizationhandler-with-multiple-registration-how-the-dependency-resolver-s
			
			//services.AddAuthorization(options => options.AddCorePolicies());
			//{
			//	options.AddPolicy(Nucleus.Core.Authorization.PagePermissionAuthorizationHandler.PAGE_VIEW_POLICY, policy =>
			//	{
			//		policy.Requirements.Add(new Nucleus.Core.Authorization.PagePermissionAuthorizationRequirement());
			//	});

			//	options.AddPolicy(Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY, policy =>
			//	{
			//		policy.Requirements.Add(new Nucleus.Core.Authorization.SiteAdminAuthorizationRequirement());
			//	});

			//	options.AddPolicy(Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY, policy =>
			//	{
			//		policy.Requirements.Add(new Nucleus.Core.Authorization.SystemAdminAuthorizationRequirement());
			//	});
			//});

			services.AddAntiforgery();


			// default response caching to no caching
			services.AddResponseCaching(options =>
			{
				options.SizeLimit = 0;
				options.MaximumBodySize = 0;
			});

			
			//services.AddMergedFileProvider(this.Configuration);

			//// This makes embedded static files in our precompiled Razor assembly [MICASAgent.WebPages.Views] work
			//services.AddRazorEmbeddedFileProviders();


		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseExceptionHandler(options => options.Use(ExceptionHandler.HandleException));

			app.UseForwardedHeaders();

			// Call .UseStaticFiles multiple times to add additional paths.  We expose specific folders only, rather than adding 
			// env.ContentRootPath so that only defined folders can serve static resources.
			foreach (string folderName in new[] { Folders.RESOURCES_FOLDER, Folders.EXTENSIONS_FOLDER, Folders.SHARED_FOLDER })
			{
				string path = System.IO.Path.Combine(env.ContentRootPath, folderName);
				
				if (System.IO.Directory.Exists(path))
				{
					app.Logger().LogInformation($"Adding static file path: [{path}]");

					app.UseStaticFiles(new StaticFileOptions
					{
						FileProvider = new PhysicalFileProvider(path),
						RequestPath = "/" + folderName
					});
				}
			}

			app.UseCookiePolicy(new CookiePolicyOptions() {  });

			app.UseRouting();

			// the order here is important.  The page routing and module routing middleware sets the Nucleus context, which is used by some of the
			// authorization handlers, but ModuleRoutingMiddleware does a permission check, which requires that Authentication has run - and middleware is executed in the order of the code below
			app.UseMiddleware<PageRoutingMiddleware>();
			app.UseAuthentication();
			app.UseMiddleware<ModuleRoutingMiddleware>();
			app.UseAuthorization();
						
			app.UseResponseCompression();

			app.UseEndpoints(routes =>
			{
				// https://github.com/aspnet/Mvc/issues/6900

				// All routes that don't match a controller/action or other defined endpoint go to the index controller and are
				// treated as CMS pages
				routes.MapFallbackToController("Index", "Default");

				// "Razor Pages" (Razor Pages is different to Razor views with controllers [MVC])
				routes.MapRazorPages();

				// map area routes for the admin controllers
				routes.MapControllerRoute(
					name: Constants.AREA_ROUTE_NAME,
					pattern: "/{area}/{controller}/{action=Index}/{id?}");

				// map routes for extension controllers
				routes.MapControllerRoute(
					name: Constants.EXTENSIONS_ROUTE_NAME,
					pattern: $"/{Constants.EXTENSIONS_ROUTE_PATH}/{{extension:exists}}/{{controller}}/{{action=Index}}/{{mid}}/{{id?}}");

				// we're not currently using this route for anything
				routes.MapControllerRoute(
					name: Constants.API_ROUTE_NAME,
					pattern: $"/{Constants.API_ROUTE_PATH}/{{extension:exists}}/{{controller}}/{{action=Index}}/{{mid}}/{{id?}}");

				// Map the search engines "site map" controller to /sitemap.xml
				routes.MapControllerRoute(
					name: Constants.SITEMAP_ROUTE_NAME,
					pattern: $"/{Constants.SITEMAP_ROUTE_PATH}",
					defaults: new {controller="Sitemap", action="Index"});

				// Configure controller routes using attribute-based routing
				routes.MapControllers();
			});
		}

		private static string ConfigFolder()
		{
			string path = System.Environment.CurrentDirectory; 

			if (!System.IO.Directory.Exists(path))
			{
				System.IO.Directory.CreateDirectory(path);
			}

			return path;
		}
	}
}
