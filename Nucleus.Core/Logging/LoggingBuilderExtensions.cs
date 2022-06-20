using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Core.Logging
{
	/// <summary>
	/// Extension methods and supporting functions used in .net core/dependency injection startup.  
	/// </summary>
	/// <remarks>
	/// Logging providers are added by Nucleus Core.Host.  You would not typically need to use these function in an Nucleus Core application.
	/// </remarks>
	public static class LoggingBuilderExtensions
	{
		public static string DataFolder { get; private set; }

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
			builder.Services.Configure<TextFileLoggerOptions>(configuration.GetSection(TextFileLoggerOptions.Section), options => options.BindNonPublicProperties=true);
			builder.Services.ConfigureOptions<ConfigureTextFileLogger>();

			builder.Services.AddSingleton<ILoggerProvider, TextFileLoggingProvider>();
			builder.Services.AddTransient<IExternalScopeProvider, LoggerExternalScopeProvider>();

			// The TextFileLogger is used by the StartupLogger for logging during dependency injection setup.  The IOptions<FolderOptions> instance which is added to the  
			// Dependency Injection container won't be configured yet when ConfigureTextFileLogger.PostConfigure is called for the instance which is used by StartupLogger,
			// so we have to read config here, and save the DataFolder in a static for use in ConfigureTextFileLogger.PostConfigure.  
			// This is not an elegant solution, but the only alternative would be to require an appSettings setting for Nucleus:TextFileLoggerOptions:Path, and we want
			// that setting to be optional.
			Nucleus.Abstractions.Models.Configuration.FolderOptions folderOptions = new();
			configuration.GetSection(Nucleus.Abstractions.Models.Configuration.FolderOptions.Section).Bind(folderOptions);
						
			DataFolder = folderOptions.DataFolder;
			if (String.IsNullOrEmpty(DataFolder))
			{
				DataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Nucleus");
			}
			DataFolder = folderOptions.Parse(DataFolder);

			return builder;
		}

		public class ConfigureTextFileLogger : IPostConfigureOptions<TextFileLoggerOptions>
		{
			private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }
			
			public ConfigureTextFileLogger(IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
			{
				this.FolderOptions = folderOptions;				
			}

			public void PostConfigure(string name, TextFileLoggerOptions options)
			{
				if (String.IsNullOrEmpty(this.FolderOptions.Value.DataFolder))
				{
					//  Special case:  When the text file logger is added to the StartupLogger, dependency injection hasn't been set up yet and folderOptions hasn't
					// been initialized, so we have to use the DataPath that we read ourselves in AddTextFileLogger.				
					this.FolderOptions.Value.DataFolder = DataFolder;

					// If the config files don't have an entry, initialize to default.  This also happens in CoreServiceExtensions.ConfigureFolderOptions.PostConfigure,
					// but that doesn't run in time for the StartupLogger
					if (String.IsNullOrEmpty(this.FolderOptions.Value.DataFolder))
					{
						CoreServiceExtensions.ConfigureFolderOptions.PostConfigure(this.FolderOptions.Value);
					}
				}

				if (String.IsNullOrEmpty(options.Path))
				{
					options.Path = this.FolderOptions.Value.GetLogFolder();
				}
			}
		}
	}
}
