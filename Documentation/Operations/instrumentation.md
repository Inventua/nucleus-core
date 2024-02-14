# Instrumentation
Metrics are application measurements reported over time. Metrics are one element of [Application Observability](https://opentelemetry.io/docs/concepts/observability-primer/), 
and are used by monitoring applications to display application activity and health information and to generate alerts based on configured thresholds. 

Nucleus (version 1.4 and later) implements both custom and built-in .Net metrics and can provide metrics data to monitoring applications which support 
[OpenTelemetry](https://opentelemetry.io/) or 
[OpenMetrics](https://github.com/OpenObservability/OpenMetrics/blob/main/specification/OpenMetrics.md).  

There are many software products which can receive and process application telemetry data, and to present that data and generate alerts.  One option is to use 
[Prometheus](https://prometheus.io/) to receive, process and store your data, and [Grafana](https://grafana.com/) to display your data and manage alerts.

See also: 
[Prometheus and Grafana](https://grafana.com/docs/grafana/latest/getting-started/get-started-grafana-prometheus/), 
[Elastic Stack - OpenTelemetry Integration](https://www.elastic.co/guide/en/observability/current/open-telemetry.html)

Nucleus publishes metrics in two ways:
- By providing a HTTP endpoint which publishes metrics which can be consumed ('scraped') by metering applications which can consume metrics in the
  [OpenMetrics](https://github.com/OpenObservability/OpenMetrics/blob/main/specification/OpenMetrics.md) 
  format.  This method is generally used when you are using [Prometheus](https://prometheus.io/).  By default, the metrics endpoint is `/_metrics`.  The 
  Nucleus metrics endpoint (url) can be customized in Nucleus configuration files.
- By periodically sending metrics to a specified OpenTelemetry [OTLP](https://opentelemetry.io/docs/specs/otlp/) collector.

> Metrics is one kind of instrumentation. The OpenTelemetry specification also includes Logging and Tracing.  OpenTelemetry Logging and Tracing are not 
currently implemented by Nucleus.

## Configuration
Instrumentation must be enabled in order to function.  To enable instrumentation with default values, edit your `appSettings.Production.json`{.file-name} 
file, and add a section to the existing Nucleus section.

```
"Nucleus" {
  "InstrumentationOptions": {
      "Meters": {
        "Enabled": true
      }
    },
  ...
}
```

See also: [Configuration Files](https://www.nucleus-cms.com/configuration-files/)

Other (optional) settings for `Nucleus:InstrumentationOptions:Meters` are:

{.table-25-75}
|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| ServiceName                      | (string)  The service name that is reported with your metrics data.   The default is `Nucleus`.  In Prometheus, this translates to a label named `Job`. | 
| ScrapeEndpointPath               | (string)  The local path of the endpoint where metrics are published.  In Prometheus, this is the path that you use when you set up "scrape" settings.  The default is `/_metrics`. | 
| OtlpEndPoint                     | (string) Specifies the OLTP/GRpc endpoint to send OpenTelemetry meters to. There is no default value.  If this value is not set, OTLP export is not enabled. | 
| CacheDuration                    | (timespan) Specifies the duration that the "scrape" telemetry content is cached before being re-generated. The default is 60 seconds.| 

```
"Nucleus" {
  "InstrumentationOptions": {
      "Meters": {
        "Enabled": true,
        "ServiceName": "www.my-site.com",
        "OtlpEndPoint": "https://my-opentelemetry-collector/v1/metrics:4317",
        "CacheDuration": "00:00:15"
      }
    },
  ...
}
```

## .Net Core built-in metrics
Nucleus enables built-in .Net metrics, and metrics which are provided by the [OpenTelemetry .Net Project](https://github.com/open-telemetry/opentelemetry-dotnet-contrib):
- [.NET extensions metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-diagnostics) 
- [ASP.NET Core metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-aspnetcore) 
- [System.Net metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-system-net) 
- [Process Metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Process-0.5.0-beta.4/src/OpenTelemetry.Instrumentation.Process/README.md#metrics)
- [.NET Runtime Metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Runtime-1.7.0/src/OpenTelemetry.Instrumentation.Process/README.md#metrics)

> Some of the metrics are provided by pre-release packages and may be subject to change.

## Nucleus Metrics

### nucleus.routing.page_visited
This counter measures the number of page visits.

{.table-25-75}
| Attribute                        |                                                             |
|----------------------------------|-------------------------------------------------------------|
| site_name                        | The name of the site which was visited.                     |
| site_id                          | The id of the site which was visited.                       |
| page_name                        | The name of the page which was visited.                     |
| page_id                          | The id of the page which was visited.                       |
| matched_route                    | The page route which was matched.                           |


### nucleus.routing.error_count
This counter measures the number of Nucleus page errors. These can be "Not Found" errors, or requests which were matched to page routes but which 
returned an error response.

{.table-25-75}
| Attribute                        |                                                                  |
|----------------------------------|------------------------------------------------------------------|
| site_name                        | The name of the site which was visited, if one could be matched. |
| site_id                          | The id of the site which was visited, if one could be matched.   |
| http_status_code                 | The HTTP status code which was returned.                         |
| http_request_path                | The local path of the failed request.                            |

### process.cpu.utilization
This gauge measures the CPU used by the Nucleus process as a percentage of the CPU units available in the system.  This gauge is not part of the 
built-in metrics provided by OpenTelemetry.Instrumentation.Process, so we publish this metric from Nucleus.

### nucleus.application.users_online
This counter measure the number of online users for each site.  An "online" user is logged in, and has site activity in the past 5 minutes.

{.table-25-75}
| Attribute                        |                                                                  |
|----------------------------------|------------------------------------------------------------------|
| site_name                        | The name of the site which was visited, if one could be matched. |
| site_id                          | The id of the site which was visited, if one could be matched.   |

### nucleus.application.environment
This gauge reports Nucleus application information.

{.table-25-75}
| Attribute                        |                                                                  |
|----------------------------------|------------------------------------------------------------------|
| start_date                       | Nucleus start date/time (UTC).                                   |
| environment_name                 | The configugured [environment](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments) name.                               |

> Nucleus enriches the built-in `http_server_request_duration` metrics by adding a route_type attribute.  The route_type attribute idenfifies which type 
of resouce was served by Nucleus.

{.table-25-75}
| route_type                       |                                                                  |
|----------------------------------|------------------------------------------------------------------|
| admin_ui                         | A response from the Nucleus administrative user interface.       |
| api                              | An API endpoint from the /api route.                             |
| error_page                       | The Nucleus error page, which is used to display user-friendly errors. |
| extension                        | Any response from an extension (that is, the /extensions route). |
| file                             | A file which is stored in a file system provider (the /files route).  |
| page                             | A site page.                                                     |
| resource                         | Static resources like css, js and image files from the /resources folder. |
| robots                           | A robots.txt file, which is automatically generated by Nucleus for use by search engines.  |
| sitemap                          | A sitemap.xml file, which is automatically generated by Nucleus for use by search engines. |

