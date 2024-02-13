using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Core.Layout;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Core.Logging;

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

    if (instrumentationOptions.Enabled)
    {
      // Enable Open Telemetry metrics and tracing
      // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
      services.AddOpenTelemetry()
        .ConfigureResource((builder) =>
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

      services.AddResourceMonitoring();
      services.AddHostedService<TelemetryMonitor>();

      services.AddScoped<TelemetryMiddleware>();
    }

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
