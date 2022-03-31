using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Nucleus.Abstractions;
using Nucleus.Core;
using Nucleus.Core.Layout;
using Nucleus.Core.Plugins;
using Nucleus.Core.Logging;
using Nucleus.Core.FileProviders;
using Nucleus.Core.DataProviders;
using Nucleus.Extensions;
using Nucleus.Core.Authentication;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions.Layout;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Nucleus.Data.Common;
using System.Linq;

namespace Nucleus.Web
{
	public class Startup
	{
		private const string HOSTING_FILENAME = "hosting.json";
		private const string CONFIG_FILENAME = "appSettings.json";
		private const string DATABASE_CONFIG_FILENAME = "databaseSettings.json";

		private const string SETTING_ENABLERESPONSECOMPRESSION = "Nucleus:EnableResponseCompression";
		private const string SETTING_ENABLEFORWARDEDHEADERS = "Nucleus:EnableForwardedHeaders";

		private IConfiguration Configuration { get; }
		private IWebHostEnvironment Environment { get; }

		public Startup(IConfiguration configuration, IWebHostEnvironment env)
		{
			this.Environment = env;

			this.Configuration = configuration; // BuildConfiguration(configuration);
		}

		private static string GetEnvironmentConfigFile(string defaultFileName, string environmentName)
		{
			return $"{System.IO.Path.GetFileNameWithoutExtension(defaultFileName)}.{environmentName}{System.IO.Path.GetExtension(defaultFileName)}";
		}

		private static void AddSingleConfigFile(List<string> configFiles, string filefullpath)
		{
			if (!configFiles.Contains(filefullpath, StringComparer.OrdinalIgnoreCase))
			{
				configFiles.Add(filefullpath);
			}
		}

		private static void AddConfigFileSet(HostBuilderContext context, List<string> configFiles, string filename)
		{
			AddSingleConfigFile(configFiles, System.IO.Path.Combine(ConfigFolder(), filename));
			AddSingleConfigFile(configFiles, System.IO.Path.Combine(ConfigFolder(), GetEnvironmentConfigFile(filename, context.HostingEnvironment.EnvironmentName)));
		}

		public static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder configurationBuilder)
		{
			List<string> configFiles = new();

			AddConfigFileSet(context, configFiles, HOSTING_FILENAME);
			AddConfigFileSet(context, configFiles, CONFIG_FILENAME);
			AddConfigFileSet(context, configFiles, DATABASE_CONFIG_FILENAME);

			// Add all of the other .json files so that users can split their configuration however they like.  We sort the filenames alphabetically
			// so that we can ensure that the files are loaded in a consistent order.  AddSingleConfigFile checks whether the file
			// already exists in the list, so we don't have to check that the config file isn't one of the ones we already added
			// above.
			foreach (string configFile in System.IO.Directory.EnumerateFiles(ConfigFolder(), "*.json").OrderBy(filename => filename, StringComparer.OrdinalIgnoreCase))
			{
				string filename = System.IO.Path.GetFileName(configFile);

				if (!filename.EndsWith(".schema.json"))
				{
					AddSingleConfigFile(configFiles, configFile);
				}
			}

			foreach (string configFile in configFiles)
			{
				configurationBuilder.AddJsonFile(configFile, optional: true, reloadOnChange: true);
			}

			configurationBuilder.AddEnvironmentVariables();
			configurationBuilder.AddCommandLine(System.Environment.GetCommandLineArgs());
		}

		public void ConfigureServices(IServiceCollection services)
		{
			// This must be called before .AddStartupLogger, because the TextFileLogger uses its values.
			services.AddFolderOptions(this.Configuration);

			services.AddStartupLogger(this.Configuration);

			services.Logger().LogInformation(new[]
			{
				$"{System.Reflection.Assembly.GetExecutingAssembly().Product()} version {System.Reflection.Assembly.GetExecutingAssembly().Version()}. {System.Reflection.Assembly.GetExecutingAssembly().Copyright()}",
				$"Application Root folder: [{System.Environment.CurrentDirectory}]",
				$"Configuration folder:    [{ConfigFolder()}]",
				$"Content Root:            [{this.Environment.ContentRootPath}]",
				$"Web Root:                [{this.Environment.WebRootPath}]",
				$"Environment:             [{this.Environment.EnvironmentName}]",
				$"Urls:                    [{this.Configuration.GetValue<string>(Microsoft.AspNetCore.Hosting.WebHostDefaults.ServerUrlsKey)}]"
			});

			services.AddHttpContextAccessor();  // required by many elements of the system
			services.AddHttpClient();

			IMvcBuilder builder = services.AddControllersWithViews();

			builder.AddRazorRuntimeCompilation();

			// services.AddLocalization(options => options.ResourcesPath = "LocalizationResources");

			// Enable logging
			services.AddLogging(logging =>
			{
				logging.ClearProviders();
				logging.AddConsole();
				logging.AddDebugLogger();
				logging.AddTextFileLogger(this.Configuration);
				logging.AddAzureWebAppDiagnostics();
			});

			services.Logger().LogInformation($"App Data Folder:         [{Core.Logging.LoggingBuilderExtensions.DataFolder}]");
				
			// Enable compression
			if (this.Configuration.GetValue<Boolean>(SETTING_ENABLERESPONSECOMPRESSION))
			{
				services.Logger().LogInformation($"Adding Response Compression");
				services.AddResponseCompression(options =>
				{
					options.Providers.Add(typeof(Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider));
					options.Providers.Add(typeof(Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider));
					options.EnableForHttps = true;
				});
			};

			if (this.Configuration.GetValue<Boolean>(SETTING_ENABLEFORWARDEDHEADERS))
			{
				services.Configure<ForwardedHeadersOptions>(options =>
				{
					options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
				});
			}

			//builder.AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver());

			// SetCompatibilityVersion is obsolete in .NET 6
			//builder.SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Latest);

			// Read HostOptions settings from config
			services.Configure<HostOptions>(Configuration.GetSection("HostOptions"));

			services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(Configuration.GetSection("FormOptions"));
			services.Configure<KestrelServerOptions>(Configuration.GetSection("KestrelServerOptions"));
			services.Configure<IISServerOptions>(Configuration.GetSection("IISServerOptions"));
			services.Configure<IISOptions>(Configuration.GetSection("IISOptions"));

			// Set (override) all of the various "max size" settings from one config value (if it is present)
			string maxRequestSize = Configuration.GetSection("Nucleus:MaxRequestSize").Value;
			if (long.TryParse(maxRequestSize, out long maxRequestSizeValue))
			{
				services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => options.MultipartBodyLengthLimit = maxRequestSizeValue);
				services.Configure<IISServerOptions>(options => { options.MaxRequestBodySize = maxRequestSizeValue; });

				// We set KestrelServerOptions options.Limits.MaxRequestBodySize to unlimited because the exception that it generates 
				// (Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException) causes problems with exception handling.  The other limits
				// encapsulate this limit anyway, and provide better error messages.
				services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = long.MaxValue; });
			}

			services.AddSecurityHeadersMiddleware(this.Configuration);

			// Add merged file provider.  
			services.AddMergedFileMiddleware(this.Configuration);

			services.AddDataProviderFactory(this.Configuration);
			services.AddCoreDataProvider(this.Configuration);

			services.Logger().LogInformation($"Adding Nucleus core services");
			services.AddNucleusCoreServices(this.Configuration);
			services.AddScoped<IContainerController, Nucleus.Web.Controllers.ContainerController>();

			builder.AddPlugins(Environment.ContentRootPath);

			services.AddRazorPages();

			services.AddCoreAuthentication(this.Configuration);
			services.AddCoreAuthorization();

			services.AddAntiforgery();

			// default response caching to no caching
			services.AddResponseCaching(options =>
			{
				options.SizeLimit = 0;
				options.MaximumBodySize = 0;
			});

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseExceptionHandler($"/{RoutingConstants.ERROR_ROUTE_PATH}");		

			if (this.Configuration.GetValue<Boolean>(SETTING_ENABLEFORWARDEDHEADERS))
			{
				app.UseForwardedHeaders();
			}

			// Call .UseStaticFiles multiple times to add additional paths.  We expose specific folders only, rather than adding 
			// env.ContentRootPath so that only defined folders can serve static resources.
			foreach (string folderName in Nucleus.Abstractions.Models.Configuration.FolderOptions.ALLOWED_STATICFILE_PATHS)
			{
				string path = System.IO.Path.Combine(env.ContentRootPath, folderName);

				if (System.IO.Directory.Exists(path))
				{
					app.Logger().LogInformation("Adding static file path: [{path}]", path);

					app.UseStaticFiles(new StaticFileOptions
					{
						FileProvider = new PhysicalFileProvider(path),
						RequestPath = "/" + folderName,
						OnPrepareResponse = context =>
						{
							if (context.Context.Response.ContentType.StartsWith("text/") && !context.Context.Response.ContentType.Contains("utf-8", StringComparison.OrdinalIgnoreCase))
							{
								context.Context.Response.ContentType += "; charset=utf-8";
							}
						}
					});
				}
			}

			app.UseAuthorizationRedirect();

			app.UseCookiePolicy(new CookiePolicyOptions() { });

			app.UseRouting();

			// the order here is important.  The page routing and module routing middleware sets the Nucleus context, which is used by some of the
			// authorization handlers, but ModuleRoutingMiddleware does a permission check, which requires that Authentication has run - and
			// middleware is executed in the order of the code below
			app.UseMiddleware<SecurityHeadersMiddleware>();
			app.UseMiddleware<MergedFileProviderMiddleware>();
			app.UseMiddleware<PageRoutingMiddleware>();
			app.UseAuthentication();
			app.UseMiddleware<Nucleus.Core.FileSystemProviders.FileIntegrityCheckerMiddleware>();
			app.UseMiddleware<SecurityHeadersMiddleware>();
			app.UseAuthorization();

			if (this.Configuration.GetValue<Boolean>(SETTING_ENABLERESPONSECOMPRESSION))
			{
				app.Logger().LogInformation($"Using Response Compression.");
				app.UseResponseCompression();
			}

			app.UseEndpoints(routes =>
			{
				// All routes that don't match a controller/action or other defined endpoint go to the index controller and are
				// treated as CMS pages.  By specifying the pattern argument (first argument) we ensure that requests that "look like"
				// filenames (that is, contains a dot) are routed to the default page controller, the standard overload uses a pattern 
				// {*path:nonfile}, which does not route those Urls to the default page controller.
				// Even though this is the first route defined, .MapFallbackToController always creates a route that is last in the
				// routing order.
				routes.MapFallbackToController("{*path}", "Index", "Default");

				// "Razor Pages" (Razor Pages is different to Razor views with controllers [MVC])
				routes.MapRazorPages();

				// Map the merged.js route
				//routes.MapControllerRoute(
				//	name: RoutingConstants.MERGED_JS_ROUTE_NAME,
				//	pattern: "/merged.js/{*src}",
				//	defaults: new { controller = "MergedFile", action = "Index" });

				// Map the merged.css route
				//routes.MapControllerRoute(
				//	name: RoutingConstants.MERGED_CSS_ROUTE_NAME,
				//	pattern: "/{linkpath}/merged.css/{*src}",
				//	defaults: new { controller = "MergedFile", action = "Index" });
				
				// Map the error page
				routes.MapControllerRoute(
					name: RoutingConstants.ERROR_ROUTE_NAME,
					pattern: $"/{RoutingConstants.ERROR_ROUTE_PATH}",
					defaults: new { controller = "Error", action = "Index" });

				// map area routes for the admin controllers
				routes.MapControllerRoute(
						name: RoutingConstants.AREA_ROUTE_NAME,
						pattern: "/{area}/{controller}/{action=Index}/{id?}");

				// map routes for extension controllers
				routes.MapControllerRoute(
					name: RoutingConstants.EXTENSIONS_ROUTE_NAME,
					pattern: $"/{RoutingConstants.EXTENSIONS_ROUTE_PATH}/{{extension:exists}}/{{controller}}/{{action=Index}}/{{mid?}}/{{id?}}");

				// we're not currently using this route for anything
				routes.MapControllerRoute(
					name: RoutingConstants.API_ROUTE_NAME,
					pattern: $"/{RoutingConstants.API_ROUTE_PATH}/{{extension:exists}}/{{controller}}/{{action=Index}}/{{mid?}}/{{id?}}");

				// Map the search engines "site map" controller to /sitemap.xml
				routes.MapControllerRoute(
					name: RoutingConstants.SITEMAP_ROUTE_NAME,
					pattern: $"/{RoutingConstants.SITEMAP_ROUTE_PATH}",
					defaults: new { controller = "Sitemap", action = "Index" });


				// Configure controller routes using attribute-based routing
				routes.MapControllers();
			});

			app.Logger().LogInformation($"Startup complete.  Nucleus is running.");
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
