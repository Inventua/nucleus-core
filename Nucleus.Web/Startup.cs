using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Nucleus.Abstractions;
using Nucleus.Core;
using Nucleus.Core.Layout;
using Nucleus.Core.Plugins;
using Nucleus.Core.Logging;
using Nucleus.Core.DataProviders;
using Nucleus.Extensions;
using Nucleus.Core.Authentication;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions.Layout;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Nucleus.Data.Common;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using System.Runtime.InteropServices;
using Nucleus.Core.Services.Instrumentation;
using Nucleus.Core.Services.HealthChecks;

namespace Nucleus.Web
{
  public class Startup
  {
    private const string HOSTING_FILENAME = "hosting";
    private const string CONFIG_FILENAME = "appSettings";
    private const string DATABASE_CONFIG_FILENAME = "databaseSettings";

    private const string CONFIG_FILE_EXTENSION = ".json";

    private const string SETTING_RESPONSECOMPRESSION_ENABLED = "Nucleus:ResponseCompression:Enabled";
    private const string SETTING_FORWARDEDHEADERS_ENABLED = "Nucleus:ForwardedHeaders:Enabled";

    private IConfiguration Configuration { get; }
    private IWebHostEnvironment Environment { get; }

    public static List<string> ConfigFiles { get; } = new();

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
      this.Environment = env;

      this.Configuration = configuration; // BuildConfiguration(configuration);
    }

    private static string GetEnvironmentConfigFile(string defaultFileName, string environmentName)
    {
      return $"{System.IO.Path.GetFileNameWithoutExtension(defaultFileName)}.{environmentName}{System.IO.Path.GetExtension(defaultFileName)}";
    }

    private static void AddSingleConfigFile(List<string> configFiles, string folder, string filename)
    {
      // Add the file, case-insensitive (check for a matching file with the SAME (case-sensitive) extension, but case-insensitive
      // filename part.  This is for Linux support (windows isn't case-sensitive), but the same code runs in both Windows/Linux.
      foreach (string foundFileFullPath in System.IO.Directory.EnumerateFiles(folder, "*" + CONFIG_FILE_EXTENSION))
      {
        string foundFileNameOnly = System.IO.Path.GetFileName(foundFileFullPath);

        if (foundFileNameOnly.Equals(filename, StringComparison.OrdinalIgnoreCase))
        {
          if (!configFiles.Contains(foundFileFullPath, StringComparer.OrdinalIgnoreCase))
          {
            configFiles.Add(foundFileFullPath);
          }
        }
      }
    }

    private static void AddConfigFileSet(HostBuilderContext context, List<string> configFiles, string filename)
    {
      AddSingleConfigFile(configFiles, ConfigFolder(), filename);
      AddSingleConfigFile(configFiles, ConfigFolder(), GetEnvironmentConfigFile(filename, context.HostingEnvironment.EnvironmentName));
    }

    public static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder configurationBuilder)
    {
      AddConfigFileSet(context, ConfigFiles, HOSTING_FILENAME + CONFIG_FILE_EXTENSION);
      AddConfigFileSet(context, ConfigFiles, CONFIG_FILENAME + CONFIG_FILE_EXTENSION);
      AddConfigFileSet(context, ConfigFiles, DATABASE_CONFIG_FILENAME + CONFIG_FILE_EXTENSION);

      // Add all of the other .json files so that users can split their configuration however they like.  We sort the filenames alphabetically
      // so that we can ensure that the files are loaded in a consistent order.  AddSingleConfigFile checks whether the file
      // already exists in the list, so we don't have to check that the config file isn't one of the ones we already added
      // above.
      foreach (string configFile in System.IO.Directory.EnumerateFiles(ConfigFolder(), "*.json").OrderBy(filename => filename, StringComparer.OrdinalIgnoreCase))
      {
        string filename = System.IO.Path.GetFileName(configFile);

        // exclude filenames with the pattern *.[environment].json
        if (!System.Text.RegularExpressions.Regex.IsMatch(filename, $"({HOSTING_FILENAME}|{CONFIG_FILENAME}|{DATABASE_CONFIG_FILENAME})\\.(?<environment>.*)\\.json", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
          if (!filename.EndsWith(".schema.json") && !filename.EndsWith(".deps.json") && !filename.EndsWith(".runtimeconfig.json") && filename != "bundleconfig.json")
          {
            AddSingleConfigFile(ConfigFiles, ConfigFolder(), filename);
          }
        }
      }

      foreach (string configFile in ConfigFiles)
      {
        configurationBuilder.AddJsonFile(configFile, optional: true, reloadOnChange: true);
      }

      configurationBuilder.AddEnvironmentVariables();
      configurationBuilder.AddCommandLine(System.Environment.GetCommandLineArgs());
    }

    public void ConfigureServices(IServiceCollection services)
    {
      try
      {
        // This must be called before .AddStartupLogger, because the TextFileLogger uses its values.
        services.AddFolderOptions(this.Configuration);
        
        services.AddStartupLogger(this.Configuration);
        
        services.Logger().LogInformation(new[]
        {
          $"{System.Reflection.Assembly.GetExecutingAssembly().Product()} version {this.GetType().Assembly.Version()}. {this.GetType().Assembly.Copyright()}",
          $"Application Root folder: [{System.Environment.CurrentDirectory}]",
          $"Configuration folder:    [{ConfigFolder()}]",
          $"Content Root:            [{this.Environment.ContentRootPath}]",
          $"Environment:             [{this.Environment.EnvironmentName}]",
          $"Urls:                    [{this.Configuration.GetValue<string>(Microsoft.AspNetCore.Hosting.WebHostDefaults.ServerUrlsKey)}]"
        });
        
        services.Logger().LogInformation("Used config files: '{file}'", String.Join(',', ConfigFiles));

        services.AddHttpContextAccessor();  // required by many elements of the system
        services.AddHttpClient();

        IMvcBuilder builder = services.AddControllersWithViews();

        builder.AddRazorRuntimeCompilation();

        // future reference:
        // services.AddLocalization(options => options.ResourcesPath = "LocalizationResources");

        // Enable logging
        services.AddLogging(logging =>
        {
          logging.ClearProviders();
          logging.AddSimpleConsole(options => { options.TimestampFormat = "dd-MMM-yyyy HH:mm:ss: "; });
          logging.AddDebugLogger();
          logging.AddTextFileLogger(this.Configuration);
          logging.AddAzureWebAppDiagnostics();
        });

        services.Logger().LogInformation($"App Data Folder:         [{this.Configuration.GetValue<String>($"{Nucleus.Abstractions.Models.Configuration.FolderOptions.Section}:DataFolder")}]");

        // Enable Open Telemetry metrics and tracing, if configured
        services.AddNucleusOpenTelemetryInstrumentation(this.Configuration);

        // Enable health checks, if configured
        services.AddNucleusHealthChecks(this.Configuration);

        // Enable compression
        if (this.Configuration.GetValue<Boolean>(SETTING_RESPONSECOMPRESSION_ENABLED))
        {
          services.Logger().LogInformation("Adding Response Compression");
          services.AddResponseCompression(options =>
          {
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.EnableForHttps = true;
          });
        };

        if (this.Configuration.GetValue<Boolean>(SETTING_FORWARDEDHEADERS_ENABLED))
        {
          services.Logger().LogInformation("Adding Forwarded Headers");
          services.Configure<ForwardedHeadersOptions>(options =>
          {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
          });
        }

        // Read HostOptions settings from config
        services.Logger().LogInformation("Setting Host Options");
        services.Configure<HostOptions>(Configuration.GetSection("HostOptions"));

        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(Configuration.GetSection("FormOptions"));
        services.Configure<KestrelServerOptions>(Configuration.GetSection("KestrelServerOptions"));
        services.Configure<IISServerOptions>(Configuration.GetSection("IISServerOptions"));
        services.Configure<IISOptions>(Configuration.GetSection("IISOptions"));

        // Set (override) all of the various "max size" settings from one config value (if it is present)
        string maxRequestSize = Configuration.GetSection("Nucleus:MaxRequestSize").Value;
        if (long.TryParse(maxRequestSize, out long maxRequestSizeValue))
        {
          services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => 
          {
            options.MultipartBodyLengthLimit = maxRequestSizeValue;

            // Make the value count limit a minimum of 2048 for the admin/files pages.  This value can be
            // further increased (but not decreased) by config (FormOptions:ValueCountLimit).
            if (options.ValueCountLimit < 2048)
            {
              options.ValueCountLimit = 2048;
            }
          });
          services.Configure<IISServerOptions>(options => { options.MaxRequestBodySize = maxRequestSizeValue; });

          // We set KestrelServerOptions options.Limits.MaxRequestBodySize to unlimited because the exception that it generates 
          // (Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException) causes problems with exception handling.  The other limits
          // encapsulate this limit anyway, and provide better error messages.
          // https://github.com/dotnet/aspnetcore/issues/23949
          services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = null; });
        }

        services.Logger().LogInformation("Adding Security Headers Middleware");
        services.AddSecurityHeadersMiddleware(this.Configuration);

        services.Logger().LogInformation("Adding Default Cache Middleware");
        services.AddDefaultCacheMiddleware(this.Configuration);

        //// Add merged file provider.  
        //services.Logger().LogInformation("Adding Merged File Middleware");
        //services.AddMergedFileMiddleware(this.Configuration);

        services.Logger().LogInformation("Adding Data Provider Services");
        services.AddDataProviderFactory(this.Configuration);
        services.AddCoreDataProvider(this.Configuration);

        services.Logger().LogInformation("Adding Nucleus core services");
        services.AddNucleusCoreServices(this.Configuration);
        services.AddScoped<IContainerController, Nucleus.Web.Controllers.ContainerController>();

        services.Logger().LogInformation("Adding Plugins");
        builder.AddPlugins(Environment.ContentRootPath);

        // For Linux only: None of the "default" behaviors work on Linux, so we have to persist to the file system.
        // This should be replaced in the future with config file options.
        // reference: https://github.com/dotnet/aspnetcore/blob/bec278eabea54f63da15e10e654bdfa4168a2479/src/DataProtection/DataProtection/src/DataProtectionBuilderExtensions.cs
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
          services.AddDataProtection()
            .SetApplicationName("Nucleus")
            .PersistKeysToFileSystem(new DirectoryInfo(Nucleus.Abstractions.Models.Configuration.FolderOptions.Parse(this.Configuration.GetValue<String>($"{Nucleus.Abstractions.Models.Configuration.FolderOptions.Section}:DataFolder"))));
        }

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

        services.CleanupDataProviderExtensions();
        services.Logger().LogInformation("Startup.ConfigureServices Complete.");
      }
      catch (Exception ex)
      {
        services.Logger().LogError(ex, "Startup.ConfigureServices Error.");
        throw;
      }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      try
      {
        app.UseNucleusOpenTelemetryEndPoint(this.Configuration, this.Environment);

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseRequestLocalization();

        // check & warn if the text file logger configuration is invalid
        Nucleus.Abstractions.Models.Configuration.TextFileLoggerOptions textFileLoggerOptions = app.ApplicationServices.GetService<Microsoft.Extensions.Options.IOptions<Nucleus.Abstractions.Models.Configuration.TextFileLoggerOptions>>().Value;
        if (!textFileLoggerOptions.Enabled)
        { 
          app.Logger().LogWarning("The text file logger was disabled because of an error accessing '{path}'.", textFileLoggerOptions.Path);
        }

        app.UseExceptionHandler($"/{RoutingConstants.ERROR_ROUTE_PATH}");

        if (this.Configuration.GetValue<Boolean>(SETTING_FORWARDEDHEADERS_ENABLED))
        {
          app.UseForwardedHeaders();
        }

        if (this.Configuration.GetValue<Boolean>(SETTING_RESPONSECOMPRESSION_ENABLED))
        {
          app.Logger().LogInformation($"Using Response Compression.");
          app.UseResponseCompression();
        }

        // Add file providers for embedded static resources in Nucleus.Web and in control panel implementations/extensions
        app.UseCompiledRazorResources(this.Environment);

        // Call .UseStaticFiles multiple times to add additional paths.  We expose specific folders only, rather than adding 
        // env.ContentRootPath so that only defined folders can serve static resources.
        app.UseStaticPhysicalPath(this.Environment);

        ////// Call .UseStaticFiles multiple times to add additional paths.  We expose specific folders only, rather than adding 
        ////// env.ContentRootPath so that only defined folders can serve static resources.
        ////foreach (string folderName in Nucleus.Abstractions.Models.Configuration.FolderOptions.ALLOWED_STATICFILE_PATHS)
        ////{
        ////  string path = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(System.IO.Path.Combine(env.ContentRootPath, folderName));

        ////  if (System.IO.Directory.Exists(path))
        ////  {
        ////    app.Logger()?.LogInformation("Adding static file path: [{path}]", "/" + folderName);

        ////    app.UseStaticFiles(new StaticFileOptions
        ////    {
        ////      FileProvider = new PhysicalFileProvider(path),
        ////      RequestPath = "/" + folderName,
        ////      OnPrepareResponse = context =>
        ////      {
        ////        // Add charset=utf-8 to content-type for text content if it is not already present
        ////        if ((context.Context.Response.ContentType.StartsWith("text/") || context.Context.Response.ContentType.StartsWith("application/javascript")) && !context.Context.Response.ContentType.Contains("utf-8", StringComparison.OrdinalIgnoreCase))
        ////        {
        ////          context.Context.Response.ContentType += "; charset=utf-8";
        ////        }

        ////        // Cache static content for 30 days
        ////        context.Context.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        ////        {
        ////          Public = true,
        ////          MaxAge = TimeSpan.FromDays(30)
        ////        };
        ////      }
        ////    });
        ////  }
        ////}

        // Set default cache-control to NoCache.  This can be overridden by controllers or middleware.
        app.UseMiddleware<DefaultNoCacheMiddleware>();

        app.UseAuthorizationRedirect();

        app.UseCookiePolicy(new CookiePolicyOptions()
        {
          Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest
        });

        app.UseRouting();

        // the order here is important.  The page routing and module routing middleware sets the Nucleus context, which is used by some of the
        // authorization handlers, but ModuleRoutingMiddleware does a permission check, which requires that Authentication has run - and
        // middleware is executed in the order of the code below
        //app.UseMiddleware<MergedFileProviderMiddleware>();
        app.UseMiddleware<PageRoutingMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<Nucleus.Core.FileSystemProviders.FileIntegrityCheckerMiddleware>();
        app.UseMiddleware<ModuleRoutingMiddleware>();
        app.UseAuthorization();
        app.UseControlPanel(this.Configuration.GetSection("Nucleus:ControlPanel:Name").Value);

        app.UseEndpoints(routes =>
        {
          // All routes that don't match a controller/action or other defined endpoint go to the index controller and are
          // treated as CMS pages.  By specifying the pattern argument (first argument) we ensure that requests that "look like"
          // filenames (that is, contain a dot) are routed to the default page controller, the standard overload uses a pattern 
          // {*path:nonfile}, which does not route those Urls to the default page controller.
          // Even though this is the first route defined, .MapFallbackToController always creates a route that is last in the
          // routing order, so any other mapped route will take precedence over this one.
          routes.MapFallbackToController(RoutingConstants.DEFAULT_PAGE_PATTERN, "Index", "Default");

          // map health check endpoint, if configured
          routes.MapNucleusHealthChecks(this.Configuration);

          // "Razor Pages" (Razor Pages is different to Razor views with controllers [MVC])
          routes.MapRazorPages();

          // Map the error page route
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

          // Map the site map controller to /sitemap.xml
          routes.MapControllerRoute(
              name: RoutingConstants.SITEMAP_ROUTE_NAME,
              pattern: $"/{RoutingConstants.SITEMAP_ROUTE_PATH}",
              defaults: new { controller = "Sitemap", action = "Index" });

          // Map the site map controller to /robots.txt
          routes.MapControllerRoute(
              name: RoutingConstants.ROBOTS_ROUTE_NAME,
              pattern: $"/{RoutingConstants.ROBOTS_ROUTE_PATH}",
              defaults: new { controller = "Sitemap", action = "Robots" });


          // Configure controller routes using attribute-based routing
          routes.MapControllers();
        });

        app.Logger().LogInformation($"Startup complete.  Nucleus is running.");
      }
      catch (Exception ex)
      {
        app.Logger().LogError(ex, "Startup.Configure Error.");
        throw;
      }
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
