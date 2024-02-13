using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Class used to read instrumentation options from configuration.
/// </summary>
public class InstrumentationOptions
{
  /// <summary>
  /// Configuration file section path for instrumentation options.
  /// </summary>
  public const string Section = "Nucleus:InstrumentationOptions:Meters";

  /// <summary>
  /// Specifies whether OpenTelemetry instrumentation is enabled.
  /// </summary>
  public Boolean Enabled { get; private set; }

  /// <summary>
  /// Service name 
  /// </summary>
  public string ServiceName { get; private set; } = "Nucleus";

  /// <summary>
  /// Specifies the Http endpoint path used to publish OpenTelemetry meters for scraping.
  /// </summary>
  /// <remarks>
  /// The default value is "/_metrics".
  /// </remarks> 
  public string ScrapeEndpointPath { get; private set; } = "/_metrics";

  /// <summary>
  /// Specifies the OLTP/GRpc endpoint to send OpenTelemetry meters to.
  /// </summary>
  /// <remarks>
  /// There is no default value.  If this value is not set, OTLP export is not enabled.
  /// </remarks> 
  public string OtlpEndPoint { get; private set; } 

  /// <summary>
  /// Specifies the duration that the "scrape" telemetry content is cached before being re-generated.
  /// </summary>
  /// <remarks>
  /// The default value is 60 seconds (00:01:00).
  /// </remarks>
  public TimeSpan CacheDuration { get; private set; } = TimeSpan.FromSeconds(60);

}
