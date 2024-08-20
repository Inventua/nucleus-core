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
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Core.Logging
{
  /// <summary>
  /// Creates an instance of the TextFileLogger.
  /// </summary>
  public class TextFileLoggingProvider : ILoggerProvider
  {
    internal DateTime LastExpiredLogsCheckDate { get; set; }
    public TextFileLoggerOptions Options { get; }
    private IExternalScopeProvider ScopeProvider { get; }
    private IHttpContextAccessor HttpContextAccessor { get; }

    public TextFileLoggingProvider(IHttpContextAccessor accessor, IOptions<TextFileLoggerOptions> TextFileLoggerOptions, IExternalScopeProvider scopeProvider)
		{
      this.HttpContextAccessor = accessor;
      this.Options = TextFileLoggerOptions.Value;
      this.ScopeProvider = scopeProvider;
    }

		public ILogger CreateLogger(string categoryName)
		{
      return new TextFileLogger(this, this.Options, this.ScopeProvider, categoryName, this.HttpContextAccessor?.HttpContext);
		}

		public void Dispose()
		{
      GC.SuppressFinalize(this);
		}
	}
}
