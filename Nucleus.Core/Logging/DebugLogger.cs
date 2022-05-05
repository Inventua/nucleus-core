using System;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Nucleus.Core.Logging
{
  /// <summary>
  /// The Debug logger writes log messages to an attached debugger.  
  /// </summary>
  /// <remarks>
  /// A Debug logger is created by the <see cref="DebugLoggingProvider"/>.  Logging is configured in appSettings.json.  This
  /// logger is used in order to write log messages in a specific format.
  /// </remarks>
  public class DebugLogger : ILogger
  {
    private string Category { get; }

    public DebugLogger(string Category)
    {
      this.Category = Category;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
      //if (this.Category.ToLower() != "microsoft.aspnetcore.localization.requestlocalizationmiddleware")
      //{
        if (IsEnabled(logLevel))
        {
          try
          {
            string strEventType;

            if (eventId.Name == null)
              strEventType = "";
            else
              strEventType = eventId.Name;

            if (exception != null)
              System.Diagnostics.Debug.WriteLine
              (
                string.Format("{0:dd-MMM-yyyy HH:mm:ss.fffffff} {1,-12} {2}", DateTime.UtcNow, "[" + logLevel.ToString() + "]", this.Category) + 
                string.Format("{0,-28} {1}", " ", formatter(state, exception) + " " + exception.Message + Environment.NewLine + exception.ToString())
              );
            else
              System.Diagnostics.Debug.WriteLine
              (
                string.Format("{0:dd-MMM-yyyy HH:mm:ss.fffffff} {1,-12} {2} {3}", DateTime.UtcNow, "[" + logLevel.ToString() + "]", this.Category, strEventType) + 
                string.Format("{0,-28} {1}", " ", formatter(state, exception))
              );
          }
          catch (Exception)
          {
          }
        }
      //}
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
      return null;
    }
  }
}
