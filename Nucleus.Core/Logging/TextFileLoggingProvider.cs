using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nucleus.Core.Logging
{
  /// <summary>
  /// Creates an instance of the TextFileLogger.
  /// </summary>
  public class TextFileLoggingProvider : ILoggerProvider
  {
    internal DateTime LastExpiredDocumentsCheckDate { get; set; }
    public TextFileLoggerOptions Options { get; }
    
    public TextFileLoggingProvider(IOptions<TextFileLoggerOptions> TextFileLoggerOptions)
		{
      this.Options = TextFileLoggerOptions.Value;
    }

		public ILogger CreateLogger(string categoryName)
		{
      return new TextFileLogger(this, Options, categoryName);
		}

		public void Dispose()
		{
      GC.SuppressFinalize(this);
		}
	}
}
