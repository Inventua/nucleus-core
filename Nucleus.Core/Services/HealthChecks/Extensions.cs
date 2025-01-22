using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Core.Logging;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Nucleus.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nucleus.Core.Plugins;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.Json;
using Microsoft.IO;
using System.Net;
using Microsoft.Extensions.Primitives;
using Nucleus.Extensions;

namespace Nucleus.Core.Services.HealthChecks;

public static class Extensions
{
  private const string SETTING_HEALTH_CHECKS_ENABLED = "Nucleus:HealthChecks:Enabled";
  private const string SETTING_HEALTH_CHECK_ENDPOINTPATH = "Nucleus:HealthChecks:HealthCheckEndPoint";
  private const string SETTING_READY_CHECK_ENDPOINTPATH = "Nucleus:HealthChecks:ReadyCheckEndPoint";
  private const string SETTING_LIVE_CHECK_ENDPOINTPATH = "Nucleus:HealthChecks:LiveCheckEndPoint";

  private const string SETTING_HEALTH_CHECKS_REQUIRE_ROLES = "Nucleus:HealthChecks:RequireRoles";

  private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new Microsoft.IO.RecyclableMemoryStreamManager();

  public static IServiceCollection AddNucleusHealthChecks(this IServiceCollection services, IConfiguration config)
  {
    if (config.GetValue<Boolean>(SETTING_HEALTH_CHECKS_ENABLED))
    {
      List<string> logEntries = [];

      services.Logger().LogInformation("Adding Health Checks");
      IHealthChecksBuilder builder = services.AddHealthChecks();

      // add all IHealthCheck implementations, including those from Nucleus core, and also any which are provided by Nucleus extensions
      foreach (Type type in AssemblyLoader.GetTypes<IHealthCheck>().Where(type => type.IsPublic))
      {
        logEntries.Add($"{type.FullName} from {type.Assembly.LogName()}");

        builder.Add
        (
          new HealthCheckRegistration
          (
            type.FullName,
            (IServiceProvider serviceProvider) => { return (IHealthCheck)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type); },
            HealthStatus.Unhealthy,
            new string[] { }
          )
        );
      }

      services.Logger().LogInformation("Added  ({count}) IHealthCheck types: '{logentries}'.", logEntries.Count, String.Join(", ", logEntries));

      // register the ApplicationReadyHealthCheck hosted service to track when the application has started
      services.AddHostedService<ApplicationReadyHealthCheck>();
    }

    return services;
  }

  public static IEndpointRouteBuilder MapNucleusHealthChecks(this IEndpointRouteBuilder builder, IConfiguration config)
  {
    if (config.GetValue<Boolean>(SETTING_HEALTH_CHECKS_ENABLED))
    {
      string[] roles = config.GetValue<String>(SETTING_HEALTH_CHECKS_REQUIRE_ROLES, "")
        .Split(new[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

      // The regular health runs every IHealthCheck implementation to check the health of Nucleus
      IEndpointConventionBuilder healthEndpointBuilder = builder.MapHealthChecks(config.GetValue<String>(SETTING_HEALTH_CHECK_ENDPOINTPATH, "/_health"), new()
      {
        ResponseWriter = WriteResponse,
        AllowCachingResponses = true
      })
      .DisableHttpMetrics();

      if (roles.Any())
      {
        healthEndpointBuilder.RequireAuthorization(options => options.RequireRole(roles));
      }

      // The "ready" health check only checks whether the application has started
      IEndpointConventionBuilder readyEndpointBuilder = builder.MapHealthChecks(config.GetValue<String>(SETTING_READY_CHECK_ENDPOINTPATH, "/_ready"), new()
      {
        Predicate = healthCheck => healthCheck.Name.Equals(typeof(ApplicationReadyHealthCheck).FullName),
        ResponseWriter = WriteResponse,
        AllowCachingResponses = true
      });

      if (roles.Any())
      {
        readyEndpointBuilder.RequireAuthorization(options => options.RequireRole(roles));
      }

      // The "live" health check doesn't actually check anything, and always returns a healthy status if the server is responding
      IEndpointConventionBuilder liveEndpointBuilder = builder.MapHealthChecks(config.GetValue<String>(SETTING_LIVE_CHECK_ENDPOINTPATH, "/_live"), new()
      {
        Predicate = healthCheck => false,
        ResponseWriter = WriteResponse,
        AllowCachingResponses = true
      });

      if (roles.Any())
      {
        liveEndpointBuilder.RequireAuthorization(options => options.RequireRole(roles));
      }
    }
    return builder;
  }

  /// <summary>
  /// Write a response in the form specified by the Accept header.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="healthReport"></param>
  /// <returns></returns>
  private static async Task WriteResponse(HttpContext context, HealthReport healthReport)
  {
    context.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
    {
      Public = true,
      MaxAge = TimeSpan.FromMinutes(1)
    };

    switch (healthReport.Status)
    {
      case HealthStatus.Healthy:
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        break;
      case HealthStatus.Degraded:
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        break;
      case HealthStatus.Unhealthy:
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        break;
    }

    if (IsMatch(context.Request.GetTypedHeaders().Accept, "text/html"))
    {
      await WriteHtmlResponse(context, healthReport);
    }
    else if (IsMatch(context.Request.GetTypedHeaders().Accept, "application/health+json", "application/json"))
    {
      await WriteJsonResponse(context, healthReport);
    }
    else
    {
      await WritePlainTextResponse(context, healthReport);
    }
  }

  private static Boolean IsMatch(IList<Microsoft.Net.Http.Headers.MediaTypeHeaderValue> acceptHeader, params string[] values)
  {
    foreach (StringSegment value in values)
    {
      if (acceptHeader.Where(header => header.MatchesMediaType(value)).Any()) return true;
    }
    return false;
  }

  /// <summary>
  /// Write a simple status response with a minimal HTML wrapper.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="healthReport"></param>
  /// <returns></returns>
  private static async Task WriteHtmlResponse(HttpContext context, HealthReport healthReport)
  {
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync($"<!DOCTYPE html><html><head><title>Health of {context.Request.Host}</title></head><body>{healthReport.Status}</body></html>");
  }

  /// <summary>
  /// Write a simple status response in plain text.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="healthReport"></param>
  /// <returns></returns>
  private static async Task WritePlainTextResponse(HttpContext context, HealthReport healthReport)
  {
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync(healthReport.Status.ToString());
  }

  /// <summary>
  /// Write a detailed response in Json format.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="healthReport"></param>
  /// <returns></returns>
  /// <remarks>
  /// This is based on sample code from <seealso href="https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks"/>.
  /// The output is intended to conform with the draft specification at <seealso href="https://datatracker.ietf.org/doc/html/draft-inadarei-api-health-check-06"/>.
  /// The draft spec appears has expired, but is the closest thing to a standard available in February 2024.   
  /// Extensions:
  /// - Each check may contain a "additional-info" array with one or more elements which each contain a "summary" and "output" property.  This is consistent with 
  ///   the specification, which allows for "Additional Keys" in section 4.10.
  /// 
  /// </remarks>
  private static async Task WriteJsonResponse(HttpContext context, HealthReport healthReport)
  {
    // If the Accept header is "application/health+json" or "application/json" then this function is called, we set the response
    // ContentType to whatever type was requested.
    context.Response.ContentType = context.Request.Headers.Accept;

    JsonWriterOptions writerOptions = new JsonWriterOptions { Indented = true };

    using (RecyclableMemoryStream memoryStream = RecyclableMemoryStreamManager.GetStream())
    {
      using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter((System.IO.Stream)memoryStream, writerOptions))
      {
        jsonWriter.WriteStartObject(); // root

        jsonWriter.WriteString("status", GetJsonHealthStatusText(healthReport.Status));
        jsonWriter.WriteString("description", $"Health of {context.Request.Host}");

        // this is the response schema version.  As the RFC proposal is expired, there isn't really a proper version 
        // for the output format, so we are using "1".
        jsonWriter.WriteString("version", "1");

        // this is the Nucleus application version
        jsonWriter.WriteString("releaseId", typeof(Extensions).Assembly.GetName().Version?.ToString());

        jsonWriter.WriteStartObject("checks");  // checks element

        foreach (KeyValuePair<string, HealthReportEntry> healthReportEntry in healthReport.Entries)
        {
          jsonWriter.WriteStartArray(healthReportEntry.Key);  // checks entry
          jsonWriter.WriteStartObject();                        // checks entry array

          jsonWriter.WriteString("status", GetJsonHealthStatusText(healthReportEntry.Value.Status));
          jsonWriter.WriteString("componentType", "component");

          // ISO8601 format
          jsonWriter.WriteString("time", DateTime.UtcNow.ToString("O"));

          if (healthReportEntry.Value.Description != null)
          {
            jsonWriter.WriteString("output", healthReportEntry.Value.Description);
          }

          if (healthReportEntry.Value.Data != null && healthReportEntry.Value.Data.Any())
          {
            jsonWriter.WriteStartArray("additional-info");          // additional-info array

            foreach (KeyValuePair<string, object> item in healthReportEntry.Value.Data)
            {
              jsonWriter.WriteStartObject();                          // additional-info array item

              jsonWriter.WriteString("summary", item.Key);

              jsonWriter.WritePropertyName("output");
              JsonSerializer.Serialize(jsonWriter, item.Value, item.Value?.GetType() ?? typeof(object));

              jsonWriter.WriteEndObject();                            // additional-info array  item
            }
            jsonWriter.WriteEndArray();                             // additional-info array
          }

          jsonWriter.WriteEndObject();                          // checks entry array
          jsonWriter.WriteEndArray();                         // checks entry
        }

        jsonWriter.WriteEndObject();            // checks element
        jsonWriter.WriteEndObject();   // root
      }

      await context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
  }

  private static string GetJsonHealthStatusText(HealthStatus status)
  {
    switch (status)
    {
      case HealthStatus.Healthy:
        return "pass";
      case HealthStatus.Degraded:
        return "warn";
      case HealthStatus.Unhealthy:
        return "fail";
      default:
        return "pass";
    }
  }
}
