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
  public const string Section = "Nucleus:InstrumentationOptions";

  /// <summary>
  /// Specifies whether OpenTelemetry instrumentation is enabled.
  /// </summary>
  public Boolean Enabled { get; private set; }

  /// <summary>
  /// Specifies the path used to publish OpenTelemetry meters.
  /// </summary>
  /// <remarks>
  /// The default value is "/_metrics".
  /// </remarks> 
  public string EndPointPath { get; private set; } = "/_metrics";

  /// <summary>
  /// Specifies the duration that the "scrape" telemetry content is cached before being re-generated.
  /// </summary>
  /// <remarks>
  /// The default value is 5 seconds (00:00:05).
  /// </remarks>
  public TimeSpan CacheDuration { get; private set; } = TimeSpan.FromSeconds(5);

}
