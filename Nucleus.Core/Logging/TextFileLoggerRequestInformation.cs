namespace Nucleus.Core.Logging;

internal class TextFileLoggerRequestInformation
{
  public string RequestPath { get; set; }
  public System.Net.IPAddress RemoteIpAddress { get; set; }  
}
