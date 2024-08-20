using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.Logging;

/// <summary>
/// Creates a special instance of the TextFileLogger for use during startup.
/// </summary>
/// <remarks>
/// The special instance sets the HttpContextAccessor to null, because there will never be a valid Http context during startup.
/// </remarks>
internal class StartupTextFileLoggingProvider : TextFileLoggingProvider
{
  public StartupTextFileLoggingProvider(IOptions<TextFileLoggerOptions> TextFileLoggerOptions, IExternalScopeProvider scopeProvider)
    : base(null, TextFileLoggerOptions, scopeProvider)
  {

  }
}
