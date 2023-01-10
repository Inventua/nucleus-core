using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Configuration;
using System.ComponentModel;
using Newtonsoft.Json;
using System.IO;

namespace Nucleus.Core.Services
{
  public class RestApiClient : Nucleus.Abstractions.IRestApiClient
  {
    private HttpClient HttpClient { get; }
   
    public RestApiClient(HttpClient httpClient)
    {
      this.HttpClient = httpClient;      
    }

    private static HttpRequestMessage CreateRequest(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, HttpContent content)
    {
      HttpRequestMessage request;

      request = new(httpMethod, new System.Uri(new(new System.Uri(baseUrl), relativePath), apiMethod))
      {
        Content = content
      };

      return request;
    }

    private static string BuildQueryString(string apiMethod,object content)
    {
      if (content != null)
      {
        Microsoft.AspNetCore.Http.Extensions.QueryBuilder builder = new
        (
          content.GetType()
            .GetProperties()
            .Select(x => new KeyValuePair<string, string>(x.Name, x.GetValue(content, null).ToString()))
        );

        apiMethod += builder.ToQueryString();
      }

      return apiMethod;
    }

    private static HttpContent BuildJson(object content)
    {
      return content == null ? null : JsonContent.Create(content);
    }

    public async Task<T> GetResponse<T>(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, object content)
    {
      if (httpMethod == HttpMethod.Get)
      {
        apiMethod = BuildQueryString(apiMethod, content);
        return await GetResponse<T>(baseUrl, relativePath, httpMethod, apiMethod, null);
      }
      else
      {
        return await GetResponse<T>(baseUrl, relativePath, httpMethod, apiMethod, BuildJson(content));
      }
    }
    
    public async Task<T> GetResponse<T>(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, HttpContent content)
    {
      // call the store to generate a new id
      HttpRequestMessage request = CreateRequest(baseUrl, relativePath, httpMethod, apiMethod, content);
      HttpResponseMessage response = await this.HttpClient.SendAsync(request);

      if (!response.IsSuccessStatusCode)
      {
        throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);        
      }

      return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
    }

    public async Task<System.IO.Stream> GetResponseAsStream(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, object content)
    {
      if (httpMethod == HttpMethod.Get)
      {
        apiMethod = BuildQueryString(apiMethod, content);
        return await GetResponseAsStream(baseUrl, relativePath, httpMethod, apiMethod, null);
      }
      else
      {
        return await GetResponseAsStream(baseUrl, relativePath, httpMethod, apiMethod, BuildJson(content));
      }
    }

    public async Task<System.IO.Stream> GetResponseAsStream(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, HttpContent content)
    {
      HttpRequestMessage request = CreateRequest(baseUrl, relativePath,httpMethod, apiMethod, content);
      HttpResponseMessage response = await this.HttpClient.SendAsync(request);

      if (!response.IsSuccessStatusCode)
      {
        throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
      }

      return await response.Content.ReadAsStreamAsync();
    }

    public async Task<string> GetResponseAsString(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, object content)
    {
      if (httpMethod == HttpMethod.Get)
      {
        apiMethod = BuildQueryString(apiMethod, content);
        return await GetResponseAsString(baseUrl, relativePath, httpMethod, apiMethod, null);
      }
      else
      {
        return await GetResponseAsString(baseUrl, relativePath, httpMethod, apiMethod, BuildJson(content));
      }
    } 

    public async Task<string> GetResponseAsString(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, HttpContent content)
    {
      HttpRequestMessage request = CreateRequest(baseUrl, relativePath, httpMethod, apiMethod, content);
      HttpResponseMessage response = await this.HttpClient.SendAsync(request);

      if (!response.IsSuccessStatusCode)
      {
        throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
      }

      return await response.Content.ReadAsStringAsync();
    }

  }
}

