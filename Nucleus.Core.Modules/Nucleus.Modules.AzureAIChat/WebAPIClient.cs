using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Sharp.MICASAgent.Abstractions;
//using MICAS.Mobile.Service.Models;
using Sharp.MICASAgent.Services;
using System.Net.Http;
using System.Threading.Tasks;

namespace MICAS.Mobile.Service.Services;

public class WebAPIClient
{  
  private const string ACCESS_KEY = "1DBAEA02-57CD-4987-8893-10D3B274E9F1";
  private const string SHARED_SECRET = "974219eb3f29499c8529142e95af02552994ed23abde4924bc4c3f9d013e6fcc";
  
  private HttpClientFactory HttpClientFactory { get; }


  public WebAPIClient(HttpClientFactory httpClientFactory) 
  {
    this.HttpClientFactory = httpClientFactory;
  }

  public class Endpoints
  {
    protected Endpoints()
    {
    }

    // GET
    public static string CheckServiceStatus { get; set; } = "api/v1.0/service/status";
    // GET
    public static string GetDeviceData { get; set; } = "api/v1.0/device/{model}/{serialnumber}/diagnostics";
    
  }


  protected virtual System.Uri GetBaseUri()
  {
    string baseUrl = "http://localhost/micaswebapi";

    if (!baseUrl.EndsWith('/'))
    {
      baseUrl += "/";
    }

    return new System.Uri(baseUrl);
  }

  protected virtual HttpClient CreateHttpClient()
  {
    HttpClient client = this.HttpClientFactory.HttpClient;
    
#if DEBUG
    // disable certificate validation (debug only)
    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
#endif
    return client;
  }

  /// <summary>
  /// Call a MICAS web API method with the specified <paramref name="relativeUri"/>, 
  /// <paramref name="method"/>, <paramref name="parameters"/> and <paramref name="content"/>.  The parameters and 
  /// content arguments can be null.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="settings"></param>
  /// <param name="relativeUri"></param>
  /// <param name="method"></param>
  /// <param name="parameters"></param>
  /// <param name="content"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="WebAPIClientException"></exception>
  public async System.Threading.Tasks.Task<T> CallMethod<T>(string relativeUri, System.Net.Http.HttpMethod method, Dictionary<string, string> parameters, string content)
  {
    System.Net.Http.HttpRequestMessage request;
    System.Net.Http.HttpResponseMessage response;
    System.Text.RegularExpressions.Match match;

    if (parameters != null)
    {
      foreach (var strKey in parameters.Keys)
      {
        relativeUri = System.Text.RegularExpressions.Regex.Replace(relativeUri, "{" + strKey + "}", System.Uri.EscapeDataString(parameters[strKey] ?? ""), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
      }
    }

    match = System.Text.RegularExpressions.Regex.Match(relativeUri, "{.*}");

    if (match.Success)
    {
      // exception: one or more parameters are missing
      string errorMessage = "";

      foreach (System.Text.RegularExpressions.Group objGroup in match.Groups)
      {
        if (!string.IsNullOrEmpty(errorMessage))
        {
          errorMessage += ",";
        }
        errorMessage += objGroup.Value;
      }

      throw new ArgumentException($"Missing Parameters: {errorMessage}");
    }

    request = new System.Net.Http.HttpRequestMessage(method, new System.Uri(this.GetBaseUri(), relativeUri));

    //if (!String.IsNullOrEmpty(settings.UserName))
    //{
    //  request.Headers.Add("X-UserName", settings.UserName);
    //}
    request.Headers.Add("X-UserName", "host");

    if (method != System.Net.Http.HttpMethod.Get && content != null)
    {
      request.Content = new System.Net.Http.StringContent(content);
      request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json");
    }

    request.Headers.Date = DateTimeOffset.Now;

    //  It's a good idea to set the user-agent.You don't have to, and it doesn't matter what you set it to, but putting
    //  something in the user-agent header gives the request signing algorithm more input to work with.
    request.Headers.UserAgent.ParseAdd(GetUserAgent());  

    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ACCESS_KEY + ":" + RequestSigningHelper.GenerateSignature(request, SHARED_SECRET));

    // we must add the accept header here, instead of using HttpClient.DefaultRequestHeaders.  This is to work around a problem
    // when the same HttpRequest object is being used in two threads, which can cause a "null" header to be added because of a
    // bug in .NET. 
    // https://github.com/dotnet/runtime/issues/24521
    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    response = await this.CreateHttpClient().SendAsync(request);

    // 20 Aug 2021 (MN) - Following line calls RetryHandler.SendAsync 
    //objResponse = await this.HttpClient.SendAsync(objRequest, new System.Net.Http.HttpCompletionOption());

    if (response.IsSuccessStatusCode)
    {
      // deserialize/parse result
      //if (GetType(T) is GetType(Object))
      //if (typeof(T) == typeof(Object))
      //{
      //  return default(T);
      //}
      
      return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
    }
    else
    {
      throw new WebAPIClientException(response);
    }
  }

  /// <summary>
  /// Return a user-agent string (to use for a Http request)
  /// </summary>
  /// <returns></returns>
  private string GetUserAgent()
  {
    return $"TEST";

    //IPlatformInfo service = DependencyService.Get<IPlatformInfo>();
    //return service.UserAgent();

  }

  //https://stackoverflow.com/questions/19260060/retrying-httpclient-unsuccessful-requests
  // Class to retry the request 3 times or simulate a timeout exception on GetResources.
  protected class RetryHandler : System.Net.Http.DelegatingHandler
  {
    // Limit the number of retries
    private const int MAX_RETRIES = 3;

    public RetryHandler() : base(new System.Net.Http.HttpClientHandler()) { }

    protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
      HttpResponseMessage response = null;
      int retryCount;

      for (retryCount = 0; retryCount < MAX_RETRIES; retryCount++)
      {
        response = await base.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
#if DEBUG
          if (request.RequestUri.ToString().Contains("resources"))
          {
            throw new TimeoutException();
          }
#endif
          return response;
        }
      }

      if (retryCount == MAX_RETRIES)
      {
        return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout);
      }
      else
      {
        return response;
      }
    }
  }

}


