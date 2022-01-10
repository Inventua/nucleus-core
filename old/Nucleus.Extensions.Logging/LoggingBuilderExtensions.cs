//using System;
//using Microsoft.Extensions.DependencyInjection;
//using System.Linq;
//using System.Reflection;
//using Microsoft.Extensions.Options;
//using Microsoft.Extensions.Logging;
////using Microsoft.AspNetCore.Builder;
////using Microsoft.AspNetCore.Hosting;
////using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
////using Microsoft.AspNetCore.Http;

//namespace Nucleus.Extensions.Logging
//{
//	/// <summary>
//	/// Extension methods and supporting functions used in .net core/dependency injection startup.  
//	/// </summary>
//	/// <remarks>
//	/// Logging providers are added by OsaAppCore.Host.  You would not typically need to use these function in an OsaAppCore application.
//	/// </remarks>
//	public static class LoggingBuilderExtensions
//	{
//		/// <summary>
//		///   Adds a Debug logger.
//		/// </summary>
//		/// <param name="builder"></param>
//		/// <returns>ILoggingBuilder</returns>
//		public static ILoggingBuilder AddDebugLogger(this ILoggingBuilder builder)
//		{
//			builder.AddProvider(new DebugLoggingProvider());

//			return builder;
//		}

//		/// <summary>
//		///   Adds a Text file logger with options defined in configuration.
//		/// </summary>
//		/// <param name="builder"></param>
//		/// <param name="configuration"></param>
//		/// <returns>ILoggingBuilder</returns>
//		public static ILoggingBuilder AddTextFileLogger(this ILoggingBuilder builder, IConfiguration configuration)
//		{
//			builder.Services.Configure<TextFileLoggerOptions>(configuration.GetSection(TextFileLoggerOptions.Section));
//			builder.Services.AddSingleton<ILoggerProvider, TextFileLoggingProvider>();


//			return builder;
//		}

//		///// <summary>
//		/////   Adds a Text file logger with default options.
//		///// </summary>
//		///// <param name="builder"></param>
//		///// <returns>ILoggingBuilder</returns>
//		//public static ILoggingBuilder AddTextFileLogger(this ILoggingBuilder builder)
//		//{
//		//	builder.AddProvider(new TextFileLoggingProvider());

//		//	return builder;
//		//}

//	}
//}
