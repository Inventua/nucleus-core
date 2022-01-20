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
			//builder.Services.Configure<TextFileLoggerOptions>(configuration.GetSection(TextFileLoggerOptions.Section));
			builder.Services.AddSingleton<ILoggerProvider, TextFileLoggingProvider>();


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
				if (String.IsNullOrEmpty(options.Path))
				{
					options.Path = this.FolderOptions.Value.GetDataFolder("Logs");
				}
			}
		}
	}
}
