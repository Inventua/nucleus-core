using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.Logging
{
  /// <summary>
  /// Extension methods and supporting functions used in .net core/dependency injection startup.  
  /// </summary>
  /// <remarks>
  /// Logging providers are added by Nucleus Core.Host.  You would not typically need to use these function in an Nucleus Core 
  /// application.
  /// </remarks>
  public static class LoggingBuilderExtensions
  {
    /// <summary>
    ///   Adds a Debug logger.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns>ILoggingBuilder</returns>
    public static ILoggingBuilder AddDebugLogger(this ILoggingBuilder builder)
    {
      builder.AddProvider(new DebugLoggingProvider());

      return builder;
    }

    /// <summary>
    ///   Adds a Text file logger with options defined in configuration.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configuration"></param>
    /// <returns>ILoggingBuilder</returns>
    public static ILoggingBuilder AddTextFileLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
      builder.Services.AddOption<TextFileLoggerOptions>(configuration, TextFileLoggerOptions.Section);

      // The TextFileLogger is used by the StartupLogger for logging during dependency injection setup.
      // The IOptions<FolderOptions> and IConfiguration instance which is added to the dependency injection container won't be
      // configured/available yet when ConfigureTextFileLogger.PostConfigure is called for the instance which is used by StartupLogger,
      // so we have to save the configuration object in a static and read the data folder setting "manually" in ConfigureTextFileLogger.
      // This is not an elegant solution, but the alternative would be to require an appSettings setting for Nucleus:TextFileLoggerOptions:Path,
      // and we want that setting to be optional.
      ConfigureTextFileLogger.Configuration = configuration;

      builder.Services.ConfigureOptions<ConfigureTextFileLogger>();
      builder.Services.AddSingleton<ILoggerProvider, TextFileLoggingProvider>();
      builder.Services.AddTransient<IExternalScopeProvider, LoggerExternalScopeProvider>();

      return builder;
    }

    /// <summary>
    ///   Adds a Startup Text file logger with options defined in configuration.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configuration"></param>
    /// <returns>ILoggingBuilder</returns>
    /// <remarks>
    /// This method should only be used by StartupLogger.cs. It creates a StartupTextFileLoggingProvider which is used
    /// during startup to log messages to text files.
    /// </remarks>
    internal static ILoggingBuilder AddStartupTextFileLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
      builder.Services.AddOption<TextFileLoggerOptions>(configuration, TextFileLoggerOptions.Section);

      // The TextFileLogger is used by the StartupLogger for logging during dependency injection setup.
      // The IOptions<FolderOptions> and IConfiguration instance which is added to the dependency injection container won't be
      // configured/available yet when ConfigureTextFileLogger.PostConfigure is called for the instance which is used by StartupLogger,
      // so we have to save the configuration object in a static and read the data folder setting "manually" in ConfigureTextFileLogger.
      // This is not an elegant solution, but the alternative would be to require an appSettings setting for Nucleus:TextFileLoggerOptions:Path,
      // and we want that setting to be optional.
      ConfigureTextFileLogger.Configuration = configuration;

      builder.Services.ConfigureOptions<ConfigureTextFileLogger>();
      builder.Services.AddSingleton<ILoggerProvider, StartupTextFileLoggingProvider>();
      builder.Services.AddTransient<IExternalScopeProvider, LoggerExternalScopeProvider>();

      return builder;
    }


    public class ConfigureTextFileLogger : IPostConfigureOptions<TextFileLoggerOptions>
    {
      private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

      // This is populated in AddTextFileLogger because we can't get an IConfiguration instance from dependency injection
      // when the text file logger is used by the StartupLogger (because Startup has not finished yet)
      internal static IConfiguration Configuration { private get; set; }
      
      public ConfigureTextFileLogger(IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
      {
        this.FolderOptions = folderOptions;
      }

      public void PostConfigure(string name, TextFileLoggerOptions options)
      {
        // Special case:
        // When the text file logger is added to the StartupLogger, dependency injection hasn't been set up yet and folderOptions hasn't
        // been initialized, so we have to read the DataPath ourselves using the Configuration value that we set in AddTextFileLogger.  We
        // set Configuration to null after use so that this code only runs for the StartupLogger.
        if (Configuration != null)
        {
          this.FolderOptions.Value.SetDataFolder(Configuration.GetValue<String>($"{Nucleus.Abstractions.Models.Configuration.FolderOptions.Section}:DataFolder"), true);
          Configuration = null;
        }

        try
        {
          options.Path = String.IsNullOrEmpty(options.Path) ? this.FolderOptions.Value.GetLogFolder() : this.FolderOptions.Value.ParseFolder(options.Path);
        }
        catch (Exception ex) when (ex is System.UnauthorizedAccessException || ex is System.IO.DirectoryNotFoundException)
        {
          // if there is a permissions error on the data/log file path, set the text file logger to disabled
          options.Enabled = false;
        }
      }
    }
  }
}
