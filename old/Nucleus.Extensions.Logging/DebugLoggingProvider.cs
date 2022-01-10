//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using System.ComponentModel;

//namespace Nucleus.Extensions.Logging
//{
//  /// <summary>
//  /// Creates an instance of the DebugLogger when required by .net core.
//  /// </summary>
//  public class DebugLoggingProvider : ILoggerProvider
//  {
//    [EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//    public ILogger CreateLogger(string categoryName)
//    {
//      return new DebugLogger(categoryName);
//    }

//    private bool disposedValue; // To detect redundant calls

//    [EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//    protected virtual void Dispose(bool disposing)
//    {
//      if (!disposedValue)
//      {
//        if (disposing)
//        {
//        }
//      }
//      disposedValue = true;
//    }

//    // TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
//    // Protected Overrides Sub Finalize()
//    // ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
//    // Dispose(False)
//    // MyBase.Finalize()
//    // End Sub

//    [EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//    public void Dispose()
//    {
//      // Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
//      Dispose(true);
//    }
//  }
//}
