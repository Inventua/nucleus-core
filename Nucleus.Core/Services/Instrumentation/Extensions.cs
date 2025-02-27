using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Core.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostic-resource-monitoring#see-also

namespace Nucleus.Core.Services.Instrumentation;

public static class Extensions
{
  /// <summary>
  /// Add Nucleus, AspNetCore, HttpClient, Runtime and Process OpenTelemetry instrumentation, if configured.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="config"></param>
  /// <returns></returns>
  public static IServiceCollection AddNucleusOpenTelemetryInstrumentation(this IServiceCollection services, IConfiguration config)
  {
    services.AddOption<InstrumentationOptions>(config, InstrumentationOptions.Section);

    InstrumentationOptions instrumentationOptions = new();    
    config.GetSection(InstrumentationOptions.Section)
      .Bind(instrumentationOptions, options => options.BindNonPublicProperties = true);

    // resource monitoring (IResourceMonitor) is used by the system information page, and is also used by OpenTelemetry, but does not work 
    // properly in Ubuntu 24.04.2 because /sys/fs/cgroup/user.slice/memory.current contains 0 shortly after a restart, and 
    // https://github.com/dotnet/extensions/blob/main/src/Libraries/Microsoft.Extensions.Diagnostics.ResourceMonitoring/Linux/LinuxUtilizationParserCgroupV2.cs 
    // line 264 throws an exception if the value is 0. Resource monitoring is initialized during .AddResourceMonitoring, so this exception is 
    // un-catchable and crashes Nucleus during startup.
    // Therefore, in Ubuntu 24.04.2 instrumentation should not be enabled. But in Windows, we want to add resource monitoring regardless of whether
    // instrumentation is enabled, so that we can use IResourceMonitor to display memory usage information in the system information page.
    
    // Add Resource Monitoring if we are running in WIndows or instrumentation is enabled
    if (instrumentationOptions.Enabled || OperatingSystem.IsWindows())
    {
      services.AddResourceMonitoring();
    }

    // only start instrumentation if it is enabled, and we are not on MacOS, because resource monitoring does
    // not work in MacOS, https://github.com/dotnet/extensions/issues/5962
    if (instrumentationOptions.Enabled && !OperatingSystem.IsMacOS())
    {
      // Enable Open Telemetry metrics and tracing
      // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
      services.AddOpenTelemetry()
        .ConfigureResource(builder =>
        {
          builder.AddService
          (
            serviceName: instrumentationOptions.ServiceName ?? "Nucleus",
            serviceVersion: typeof(Extensions).Assembly.GetName().Version?.ToString() ?? null,
            serviceInstanceId: Environment.MachineName
          );
        })
        
        .WithMetrics(builder =>
        {
          builder.AddAspNetCoreInstrumentation();
          builder.AddHttpClientInstrumentation();
          builder.AddRuntimeInstrumentation();
          builder.AddProcessInstrumentation();
          builder.AddMeter("nucleus*", "aspnetcore*", "process*", "dotnet*");
          builder.AddPrometheusExporter(options =>
          {
            options.ScrapeEndpointPath = instrumentationOptions.ScrapeEndpointPath ?? "/_metrics";
            options.ScrapeResponseCacheDurationMilliseconds = (int)instrumentationOptions.CacheDuration.TotalMilliseconds;
          });
         
          if (!String.IsNullOrEmpty(instrumentationOptions.OtlpEndPoint))
          {
            if (Uri.TryCreate(instrumentationOptions.OtlpEndPoint, UriKind.Absolute, out Uri otlpTargetUri))
            {
              builder.AddOtlpExporter("metrics", options =>
              {
                options.Endpoint = otlpTargetUri;
              });
            }
            else
            {
              // MetricsOltpTargetEndPoint is set, but the value is not a valid Uri
              services.Logger().LogWarning("The configured value for {configSection}:{configProperty} '{value}' is not a valid Uri.", InstrumentationOptions.Section, nameof(InstrumentationOptions.OtlpEndPoint), instrumentationOptions.OtlpEndPoint);
            }
          }
        });

      // Not currently enabled, for later use
      //.WithTracing(builder =>
      //{
      //  builder.AddHttpClientInstrumentation();
      //  builder.AddAspNetCoreInstrumentation();
      //  builder.AddOtlpExporter();
      //});

      services.AddHostedService<TelemetryMonitor>();

      services.AddSingleton<TelemetryMiddleware>();
    }

    // we add resource monitoring regardless of config file settings, because we use IResourceMonitor in the system information page
    //services.AddResourceMonitoring();

    return services;
  }

  /// <summary>
  /// Start OpenTelemetry services.
  /// </summary>
  /// <param name="builder"></param>
  /// <param name="config"></param>
  /// <param name="environment"></param>
  /// <returns></returns>
  public static IApplicationBuilder UseNucleusOpenTelemetryEndPoint(this IApplicationBuilder builder, IConfiguration config, IWebHostEnvironment environment)
  {
    InstrumentationOptions instrumentationOptions = new();
    config.GetSection(InstrumentationOptions.Section)
      .Bind(instrumentationOptions, options => options.BindNonPublicProperties = true);

    if (instrumentationOptions.Enabled)
    {
      builder.UseOpenTelemetryPrometheusScrapingEndpoint();
      builder.UseMiddleware<TelemetryMiddleware>();           
    }
    return builder;
  }

  // Not currently enabled, for later use
  //public static ILoggingBuilder AddNucleusOpenTelemetryLogging(this ILoggingBuilder builder, IConfiguration config)
  //{
  //  // Enable Open Telemetry logger
  //  builder.AddOpenTelemetry(options =>
  //  {
  //    options.SetResourceBuilder(
  //      ResourceBuilder
  //        .CreateDefault()
  //        .AddService(
  //          serviceName: "Nucleus",
  //          serviceVersion: typeof(Extensions).Assembly.GetName().Version?.ToString() ?? "unknown",
  //          serviceInstanceId: Environment.MachineName));

  //    options.IncludeScopes = true;
  //    options.IncludeFormattedMessage = true;
  //    options.ParseStateValues = true;

  //    options.AddOtlpExporter(exporterOptions =>
  //    {
  //      exporterOptions.Endpoint = new("???");
  //    });
  //  });

  //  return builder;
  //}
}
