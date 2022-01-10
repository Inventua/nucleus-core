//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using System.Security;
//using System.Text;
//using System.Threading.Tasks;
//using System.ComponentModel;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;

//namespace Nucleus.Extensions.Logging
//{
//  /// <summary>
//  /// Creates an instance of the TextFileLogger when required by .net core.
//  /// </summary>
//  public class TextFileLoggingProvider : ILoggerProvider
//  {
//    internal DateTime LastExpiredDocumentsCheckDate { get; set; }
//    internal TextFileLoggerOptions Options { get; }
    
//    [EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//    public TextFileLoggingProvider(IOptions<TextFileLoggerOptions> TextFileLoggerOptions)
//		{
//      this.Options = TextFileLoggerOptions.Value;
//    }

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public ILogger CreateLogger(string categoryName)
//		{
//      //if (this.HttpContextAccessor != null && this.HttpContextAccessor.HttpContext != null && this.HttpContextAccessor.HttpContext.Request != null)
//      //{
//      //  string deviceId = Inventua.Sharp.SAM.Services.SharpOsa.SharpMfpMiddleware.GetDeviceId(this.HttpContextAccessor.HttpContext);
//      //  return new TextFileLogger(this, categoryName);
//      //}
//      return new TextFileLogger(this, Options, categoryName);
//		}

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public void Dispose()
//		{

//		}
//	}
//}
