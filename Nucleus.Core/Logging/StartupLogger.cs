using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

namespace Nucleus.Core.Logging
{
	/// <summary>
	/// Creates a logger for use during startup, and exposes the logger as an extension to DI startup classes.
	/// </summary>
	/// <remarks>
	/// The logger is available as an extension method to the IMvcBuilder, IApplicationBuilder and IServiceCollection classes
	/// so that it can only be used by startup and configuration methods (and can't be used by the main application, which should
	/// use loggers provided by dependency injection)
	/// </remarks>
	public static class StartupLoggerExtensions
	{		
		private static ILogger _startupLogger;
		
		/// <summary>
		/// Create logger for use during startup
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		public static void AddStartupLogger(this IServiceCollection services, IConfiguration configuration)
		{
      // Create loggers for logging during startup
      using var loggerFactory = LoggerFactory.Create(builder =>
      {
        builder.AddSimpleConsole(options => { options.TimestampFormat = "dd-MMM-yyyy HH:mm:ss: "; });
        builder.AddDebug();
        builder.AddStartupTextFileLogger(configuration);
      });

      _startupLogger = loggerFactory.CreateLogger("Startup");
    }

    private static ILogger CreateLogger()
    {
      // Create loggers for logging during startup
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddSimpleConsole(options => { options.TimestampFormat = "dd-MMM-yyyy HH:mm:ss: "; });
				builder.AddDebug();
			});

			_startupLogger = loggerFactory.CreateLogger("Startup");

      return _startupLogger;
    }

    public static ILogger GetLogger()
    {
    
        return _startupLogger ?? CreateLogger();
      
    }

		/// <summary>
		/// Gets the startup logger
		/// </summary>
		/// <param name="builder"><see cref="IMvcBuilder"/> instance.</param>
		/// <returns>Startup logger.</returns>
		public static ILogger Logger(this IMvcBuilder builder, [CallerMemberName] string caller = "")
		{
      return GetLogger(); // _startupLogger;
		}

		/// <summary>
		/// Gets the startup logger
		/// </summary>
		/// <param name="builder"><see cref="IApplicationBuilder"/> instance.</param>
		/// <returns>Startup logger.</returns>
		public static ILogger Logger(this IApplicationBuilder builder, [CallerMemberName] string caller = "")
		{
      return GetLogger(); // _startupLogger;
    }

		/// <summary>
		/// Gets the startup logger
		/// </summary>
		/// <param name="services"><see cref="IServiceCollection"/> instance.</param>
		/// <returns>Startup logger.</returns>
		public static ILogger Logger(this IServiceCollection services, [CallerMemberName] string caller = "")
		{
			return GetLogger(); // _startupLogger;
    }

		/// <summary>
		/// Gets the startup logger
		/// </summary>
		/// <param name="host"><see cref="IHost"/> instance.</param>
		/// <returns>Startup logger.</returns>
		public static ILogger Logger(this IHost host, [CallerMemberName] string caller = "")
		{
      return GetLogger(); // _startupLogger;
    }

    /// <summary>
    /// Gets the startup logger
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> instance.</param>
    /// <returns>Startup logger.</returns>
    public static ILogger Logger(this IHostBuilder builder, [CallerMemberName] string caller = "")
		{
			return GetLogger(); // _startupLogger;
    }
		
		/// <summary>
		/// Extension for the <see cref="ILogger.LogInformation"/> method to log multiple lines in one log entry.
		/// </summary>
		/// <param name="logger">Logger that this method extends.</param>
		/// <param name="messages">String array of messages.</param>
		/// <param name="args">Values to substitute in messages.</param>
		public static void LogInformation(this ILogger logger, string[] messages)
		{
			foreach (string message in messages)
			{
				logger.LogInformation(message);
			}
		}
	}		
}
