using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Core.Layout;

namespace Nucleus.Core.Services.Instrumentation;

public class TelemetryMiddleware : IMiddleware
{  
  private Counter<int> PagesVisitedCounter { get; }
  private Counter<int> ErrorCounter { get; }

  private enum TelemetryRouteTypes : int
  {
    Unknown,
    Robots,
    Sitemap,
    Admin_UI,
    Api,
    Error_Page,
    Extension,
    Page,
    File,
    Resource
  }

  public TelemetryMiddleware(IMeterFactory meterFactory)
  {
    Meter pagesVisitedMeter = meterFactory.Create("nucleus.routing", typeof(PageRoutingMiddleware).Assembly.GetName().Version.ToString());
    this.PagesVisitedCounter = pagesVisitedMeter.CreateCounter<int>("nucleus.routing.page_visited", description: "Nucleus pages visited.");
    this.ErrorCounter = pagesVisitedMeter.CreateCounter<int>("nucleus.routing.error_count", description: "Nucleus error responses.");
  }

  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    // process the request before publishing metrics 
    await next(context);

    Context nucleusContext = context.RequestServices.GetService<Context>();

    if (!HttpStatusCodeIsSuccess(context.Response.StatusCode))
    {
      // publish error counters
      this.ErrorCounter.Add(1,
        new KeyValuePair<string, object>("site_name", nucleusContext?.Site?.Name),
        new KeyValuePair<string, object>("site_id", nucleusContext?.Site?.Id),
        new KeyValuePair<string, object>("http_status_code", context.Response.StatusCode),
        new KeyValuePair<string, object>("http_request_path", context.Request.Path.ToString()));
    }
    else 
    {
      if (nucleusContext?.Site != null && nucleusContext?.Page != null)
      {
        // publish page visits
        this.PagesVisitedCounter.Add(1,
          new KeyValuePair<string, object>("site_name", nucleusContext.Site?.Name),
          new KeyValuePair<string, object>("site_id", nucleusContext.Site?.Id),
          new KeyValuePair<string, object>("page_name", nucleusContext.Page?.Name),
          new KeyValuePair<string, object>("page_id", nucleusContext.Page?.Id),
          new KeyValuePair<string, object>("matched_route", nucleusContext?.MatchedRoute?.Path)
        );
      }

      // for GET requests only, enrich the ASP.NET Core request metric with a route_type, and also populate the http_route label
      // with the "default" page pattern because the MapFallbackToController route which we use to publish pages doesn't get a
      // http_route assigned.
      if (context.Request.Method == "GET")
      {
        IHttpMetricsTagsFeature tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
        if (tagsFeature != null)
        {
          TelemetryRouteTypes routeType = TelemetryRouteTypes.Unknown;
          Microsoft.AspNetCore.Routing.RouteEndpoint endPoint = context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint;
          if (endPoint != null)
          {
            string routeName = endPoint?.Metadata.OfType<RouteNameMetadata>().SingleOrDefault()?.RouteName;
            routeType = routeName switch
            {
              RoutingConstants.ROBOTS_ROUTE_NAME => TelemetryRouteTypes.Robots,
              RoutingConstants.SITEMAP_ROUTE_NAME => TelemetryRouteTypes.Sitemap,
              RoutingConstants.AREA_ROUTE_NAME => TelemetryRouteTypes.Admin_UI,
              RoutingConstants.API_ROUTE_NAME => TelemetryRouteTypes.Api,
              RoutingConstants.ERROR_ROUTE_NAME => TelemetryRouteTypes.Error_Page,
              RoutingConstants.EXTENSIONS_ROUTE_NAME => TelemetryRouteTypes.Extension,
              _ => TelemetryRouteTypes.Unknown
            };
          }

          if (routeType == TelemetryRouteTypes.Unknown)
          {
            routeType = DetermineType(context.Request, nucleusContext);
          }

          if (routeType != TelemetryRouteTypes.Unknown)
          {
            // add a route_type label to telemetry to specify what kind of response was returned
            tagsFeature.Tags.Add(new KeyValuePair<string, object>("route_type", routeType.ToString().ToLower()));

            if (routeType == TelemetryRouteTypes.Page)
            {
              // the MapFallbackToController route doesn't get a http_route label in telemetry, so we add it ourselves
              tagsFeature.Tags.Add(new KeyValuePair<string, object>("http_route", RoutingConstants.DEFAULT_PAGE_PATTERN));
            }
          }
        }
      }
    }
  }

  /// <summary>
  /// Determine route types for routes which can not be detected by route name.
  /// </summary>
  /// <param name="request"></param>
  /// <returns></returns>
  private TelemetryRouteTypes DetermineType(HttpRequest request, Context nucleusContext)
  {
    if (nucleusContext.Page != null) return TelemetryRouteTypes.Page;
    if (request.Path.StartsWithSegments("/" + RoutingConstants.FILES_ROUTE_PATH_PREFIX)) return TelemetryRouteTypes.File;

    // check for static resources
    string[] pathParts = request.Path.ToString().Split(new char[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

    // check to see if the first part of the path is Request.PathBase.  If it is, use the next part of the path as the root 
    // path to check (this is what happens when an application is run in an IIS virtual directory)
    string pathStart = pathParts.FirstOrDefault()?.Equals(request.PathBase, StringComparison.OrdinalIgnoreCase) == true ? pathParts.Skip(1).FirstOrDefault() : pathParts.FirstOrDefault();
    if (pathStart != null && FolderOptions.ALLOWED_STATICFILE_PATHS.Contains(pathStart, StringComparer.OrdinalIgnoreCase))
    {
      return TelemetryRouteTypes.Resource;
    }

    return TelemetryRouteTypes.Unknown;
  }

  /// <summary>
  /// Return whether the status code is a success code.  For the purposes of this module, status codes in the "300" range 
  /// are regarded as success codes as well as those in the 200 range.
  /// </summary>
  /// <param name="statusCode"></param>
  /// <returns></returns>
  private static Boolean HttpStatusCodeIsSuccess(int statusCode)
  {
    return statusCode >= (int)System.Net.HttpStatusCode.OK && statusCode < (int)System.Net.HttpStatusCode.BadRequest;
  }
}
