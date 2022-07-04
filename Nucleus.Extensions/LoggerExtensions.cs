using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;

namespace Nucleus.Extensions.Logging
{
	/// <summary>
	/// The LoggerExtensions class wraps the .net core ILogger extensions functions and checks IsEnabled(logLevel) before 
	/// calling Log functions in order to avoid unnecessary object creation and memory usage.
	/// </summary>
	/// <remarks>
	/// To use, reference Nucleus.Extensions and add a 'using Nucleus.Extensions.Logging;' line after the 
	/// 'using Microsoft.Extensions.Logging;' line.
	/// </remarks>
	public static class LoggerExtensions 
	{
		/// <inheritdoc/>
		public static void LogInformation(this Microsoft.Extensions.Logging.ILogger logger, string message)
		{
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.Log(LogLevel.Information, message);
			}
		}

		/// <inheritdoc/>
		public static void LogInformation<T0>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0)
		{
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.Log(LogLevel.Information, message, arg0);
			}
		}

		/// <inheritdoc/>
		public static void LogInformation<T0,T1>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0, T1 arg1)
		{
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.Log(LogLevel.Information, message, arg0, arg1);
			}
		}

		/// <inheritdoc/>
		public static void LogInformation<T0, T1,T2>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2)
		{
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.Log(LogLevel.Information, message, arg0, arg1, arg2);
			}
		}

		/// <inheritdoc/>
		public static void LogWarning(this Microsoft.Extensions.Logging.ILogger logger, string message)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.Log(LogLevel.Warning, message);
			}
		}

		/// <inheritdoc/>
		public static void LogWarning<T0>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.Log(LogLevel.Warning, message, arg0);
			}
		}

		/// <inheritdoc/>
		public static void LogWarning<T0, T1>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0, T1 arg1)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.Log(LogLevel.Warning, message, arg0, arg1);
			}
		}

		/// <inheritdoc/>
		public static void LogWarning<T0, T1, T2>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.Log(LogLevel.Warning, message, arg0, arg1, arg2);
			}
		}

		/// <inheritdoc/>
		public static void LogTrace(this Microsoft.Extensions.Logging.ILogger logger, string message)
		{
			if (logger.IsEnabled(LogLevel.Trace))
			{
				logger.Log(LogLevel.Trace, message);
			}
		}

		/// <inheritdoc/>
		public static void LogTrace<T0>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0)
		{
			if (logger.IsEnabled(LogLevel.Trace))
			{
				logger.Log(LogLevel.Trace, message, arg0);
			}
		}

		/// <inheritdoc/>
		public static void LogTrace<T0, T1>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0, T1 arg1)
		{
			if (logger.IsEnabled(LogLevel.Trace))
			{
				logger.Log(LogLevel.Trace, message, arg0, arg1);
			}
		}

		/// <inheritdoc/>
		public static void LogTrace<T0, T1, T2>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2)
		{
			if (logger.IsEnabled(LogLevel.Trace))
			{
				logger.Log(LogLevel.Trace, message, arg0, arg1, arg2);
			}
		}

		/// <inheritdoc/>
		public static void LogDebug(this Microsoft.Extensions.Logging.ILogger logger, string message)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.Log(LogLevel.Debug, message);
			}
		}

		/// <inheritdoc/>
		public static void LogDebug<T0>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.Log(LogLevel.Debug, message, arg0);
			}
		}

		/// <inheritdoc/>
		public static void LogDebug<T0, T1>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0, T1 arg1)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.Log(LogLevel.Debug, message, arg0, arg1);
			}
		}

		/// <inheritdoc/>
		public static void LogDebug<T0, T1, T2>(this Microsoft.Extensions.Logging.ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.Log(LogLevel.Debug, message, arg0, arg1, arg2);
			}
		}

		// //


		/// <inheritdoc/>
		public static void LogError(this Microsoft.Extensions.Logging.ILogger logger, Exception exception, string message)
		{
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.Log(LogLevel.Error, exception, message);
			}
		}

		/// <inheritdoc/>
		public static void LogError<T0>(this Microsoft.Extensions.Logging.ILogger logger, Exception exception, string message, T0 arg0)
		{
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.Log(LogLevel.Error, exception, message, arg0);
			}
		}

		/// <inheritdoc/>
		public static void LogError<T0, T1>(this Microsoft.Extensions.Logging.ILogger logger, Exception exception, string message, T0 arg0, T1 arg1)
		{
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.Log(LogLevel.Error, exception, message, arg0, arg1);
			}
		}

		/// <inheritdoc/>
		public static void LogError<T0, T1, T2>(this Microsoft.Extensions.Logging.ILogger logger, Exception exception, string message, T0 arg0, T1 arg1, T2 arg2)
		{
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.Log(LogLevel.Error, exception, message, arg0, arg1, arg2);
			}
		}
	}
}
