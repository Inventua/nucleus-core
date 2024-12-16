using System;
using System.Net.Http;
using System.Threading.Tasks;
using Nucleus.Abstractions.Conversion;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.TikaServerConverter;

public class TikaServerContentConverter : IContentConverter
{
  private IHttpClientFactory HttpClientFactory { get; }

  public TikaServerContentConverter(IHttpClientFactory httpClientFactory)
  {
    this.HttpClientFactory = httpClientFactory;
  }

  public IContentConverter.Weights Weight(string sourceContentType, string targetContentType)
  {
    switch (targetContentType)
    {
      case "text/plain":  // tika can only convert to text
        switch (sourceContentType)
        {
          // assert that Tika is "best" at converting specific formats to text. These are types that are "likely" to be uploaded to a site, and that
          // are documented as being supported by Tika at https://tika.apache.org/3.0.0/formats.html
          case "application/pdf":

          // old office formats
          case "application/msword":                
          case "application/vnd.ms-excel":
          case "application/vnd.ms-powerpoint":
          
          // modern office formats
          case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":  
          case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
          case "application/vnd.openxmlformats-officedocument.presentationml.presentation":

          // openoffice
          case "application/vnd.oasis.opendocument.presentation":
          case "application/vnd.oasis.opendocument.spreadsheet":
          case "application/vnd.oasis.opendocument.text":
          
          // common generic formats
          case "application/rtf":
            return IContentConverter.Weights.Best; 
        }

        // Tika can extract text from thousands of formats, so we assert that it can convert "all" formats to text - the code above is just to
        // boost the weight of the Tika-server converter for formats that we know are well-handled.
        return IContentConverter.Weights.Capable;
    }
    

    return IContentConverter.Weights.NotCapable;
  }
  
  public async Task<byte[]> ConvertTo(Site site, byte[] content, string sourceContentType, string targetContentType)
  {
    Models.Settings settings = new();
    settings.GetSettings(site);

    if (!String.IsNullOrEmpty(settings.TikaServerEndpoint))
    {
      HttpClient client = this.HttpClientFactory.CreateClient();
      HttpRequestMessage request = new(HttpMethod.Put, new System.Uri(new System.Uri(settings.TikaServerEndpoint), "/tika"));
      client.Timeout = TimeSpan.FromMinutes(5);

      request.Content = new ByteArrayContent(content);
      request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(sourceContentType);     
      request.Headers.Accept.Add(new(targetContentType));

      HttpResponseMessage response = client.Send(request);

      response.EnsureSuccessStatusCode();

      return await response.Content.ReadAsByteArrayAsync();
    }
    else
    {
      throw new InvalidOperationException("Tika Server endpoint is not set.");
    }  

  }
}
 