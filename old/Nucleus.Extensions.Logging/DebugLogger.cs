//using System;
//using Microsoft.Extensions.Logging;
//using System.ComponentModel;

//namespace Nucleus.Extensions.Logging
//{
//  /// <summary>
//  /// The Debug logger writes log messages to an attached debugger.  
//  /// </summary>
//  /// <remarks>
//  /// A Debug logger is created by the <see cref="DebugLoggingProvider"/>.  Logging is configured in /config/appSettings.json.
//  /// </remarks>
//  public class DebugLogger : ILogger
//  {
//    private string Category { get; }

//    [EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//    public DebugLogger(string Category)
//    {
//      this.Category = Category;
//    }

//    [EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//    {
//      //if (this.Category.ToLower() != "microsoft.aspnetcore.localization.requestlocalizationmiddleware")
//      //{
//        if (IsEnabled(logLevel))
//        {
//          try
//          {
//            string strEventType;

//            if (eventId.Name == null)
//              strEventType = "";
//            else
//              strEventType = eventId.Name;

//            if (exception != null)
//              System.Diagnostics.Debug.WriteLine
//              (
//                string.Format("{0:dd-MMM-yyyy HH:mm:ss.fffffff} {1,-12} {2}", DateTime.Now, "[" + logLevel.ToString() + "]", this.Category) + Environment.NewLine + string.Format("{0,-28} {1}", " ", formatter(state, exception) + " " + exception.Message + Environment.NewLine + exception.ToString())
//              );
//            else
//              System.Diagnostics.Debug.WriteLine
//              (
//                string.Format("{0:dd-MMM-yyyy HH:mm:ss.fffffff} {1,-12} {2} {3}", DateTime.Now, "[" + logLevel.ToString() + "]", this.Category, strEventType) + Environment.NewLine + string.Format("{0,-28} {1}", " ", formatter(state, exception))
//              );
//          }
//          catch (Exception)
//          {
//          }
//        }
//      //}
//    }

//    [EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//    public bool IsEnabled(LogLevel logLevel)
//    {
//      return true;
//    }

//    [EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//    public IDisposable BeginScope<TState>(TState state)
//    {
//      return null;
//    }
//  }
//}
