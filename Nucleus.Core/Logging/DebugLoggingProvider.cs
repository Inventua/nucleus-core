using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Nucleus.Core.Logging
{
  /// <summary>
  /// Creates an instance of the DebugLogger when required by .net core.
  /// </summary>
  public class DebugLoggingProvider : ILoggerProvider
  {
    public ILogger CreateLogger(string categoryName)
    {
      return new DebugLogger(categoryName);
    }

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
        }
      }
      disposedValue = true;
    }

    public void Dispose()
    {
      // Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
      Dispose(true);
    }
  }
}
