using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Core;

/// <summary>
/// IConfigurationSource which creates an instance of EnvironmentVariableReplacementsConfigurationProvider, which is used to 
/// override configuration file settings with values where environment variable tokens have been replaced with the
/// environment variable values.
/// </summary>
internal class ExpandEnvironmentVariablesConfigurationSource : IConfigurationSource
{
  private Dictionary<string, string> Settings { get; }

  /// <summary>
  /// Constructor.
  /// </summary>
  /// <param name="settings">
  /// A Dictionary of setting keys and values to override.
  /// </param>
  public ExpandEnvironmentVariablesConfigurationSource(Dictionary<string, string> settings)
  {
    this.Settings = settings;
  }

  public IConfigurationProvider Build(IConfigurationBuilder builder)
  {
    return new ExpandEnvironmentVariablesConfigurationProvider(this.Settings);
  }
}

/// <summary>
/// ConfigurationProvider which is used to override configuration file settings with values where environment variable tokens
/// have been replaced with the environment variable values.
/// </summary>
internal class ExpandEnvironmentVariablesConfigurationProvider : ConfigurationProvider
{
  private Dictionary<string, string> Settings { get; }

  /// <summary>
  /// Constructor.
  /// </summary>
  /// <param name="settings">
  /// A Dictionary of setting keys and values to override.
  /// </param>
  public ExpandEnvironmentVariablesConfigurationProvider(Dictionary<string, string> settings)
  {
    this.Settings = settings;
  }

  public override void Load()
  {
    this.Data = this.Settings;
  }
}
