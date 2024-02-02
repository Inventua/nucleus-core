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
using OpenTelemetry.Logs;
using static Nucleus.Core.Logging.LoggingBuilderExtensions;
using Microsoft.AspNetCore.Builder;
using Google.Protobuf.WellKnownTypes;

// https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostic-resource-monitoring#see-also

namespace Nucleus.Core.Services.Instrumentation;

public static class Extensions
{
  public static IServiceCollection AddNucleusOpenTelemetryInstrumentation(this IServiceCollection services, IConfiguration config)
  {
    services.AddOption<Nucleus.Abstractions.Models.Configuration.InstrumentationOptions>(config, Nucleus.Abstractions.Models.Configuration.InstrumentationOptions.Section);

    Nucleus.Abstractions.Models.Configuration.InstrumentationOptions instrumentationOptions = config.GetSection("Nucleus:InstrumentationOptions").Get<Nucleus.Abstractions.Models.Configuration.InstrumentationOptions>(options => options.BindNonPublicProperties = true);

    if (instrumentationOptions.Enabled)
    {
      // Enable Open Telemetry metrics and tracing
      // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
      services.AddOpenTelemetry()
        .ConfigureResource((builder) =>
        {
          builder.AddService
          (
            serviceName: "Nucleus",
            serviceVersion: typeof(Extensions).Assembly.GetName().Version?.ToString() ?? "unknown",
            serviceInstanceId: Environment.MachineName
          );
        })
        .WithMetrics(builder =>
        {
          builder.AddAspNetCoreInstrumentation();
          builder.AddHttpClientInstrumentation();
          builder.AddMeter("nucleus*", "aspnetcore*", "process*", "dotnet*");
          builder.AddPrometheusExporter(options =>
          {
            options.ScrapeEndpointPath = instrumentationOptions.EndPointPath ?? "/_metrics";
            options.ScrapeResponseCacheDurationMilliseconds = (int)instrumentationOptions.CacheDuration.TotalMilliseconds;
          });
        });
      //.WithTracing(builder =>
      //{
      //  builder.AddHttpClientInstrumentation();
      //  builder.AddAspNetCoreInstrumentation();
      //  builder.AddOtlpExporter();
      //});

      services.AddResourceMonitoring();
      services.AddHostedService<ResourceMonitor>();
    }

    return services;
  }

  public static IApplicationBuilder UseNucleusOpenTelemetryEndPoint(this IApplicationBuilder builder, IConfiguration config)
  {
    if (config.GetValue<Boolean>("Nucleus:InstrumentationOptions:Enabled"))
    {
      builder.UseOpenTelemetryPrometheusScrapingEndpoint();
    }
    return builder;
  }

  // Not currently enabled
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
