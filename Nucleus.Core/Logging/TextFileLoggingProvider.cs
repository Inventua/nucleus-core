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
    private IExternalScopeProvider ScopeProvider { get; set; }

    public TextFileLoggingProvider(IOptions<TextFileLoggerOptions> TextFileLoggerOptions, IExternalScopeProvider scopeProvider)
		{
      this.Options = TextFileLoggerOptions.Value;
      this.ScopeProvider = scopeProvider;
    }

		public ILogger CreateLogger(string categoryName)
		{
      return new TextFileLogger(this, this.Options, this.ScopeProvider, categoryName);
		}

		public void Dispose()
		{
      GC.SuppressFinalize(this);
		}
	}
}
