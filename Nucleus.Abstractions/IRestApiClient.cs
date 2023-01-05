using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
  /// <summary>
  /// General-purpose Api client.
  /// </summary>
  public interface IRestApiClient
  {
    /// <summary>
    /// Send a Http request and return the response as the specified type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="baseUrl"></param>
    /// <param name="relativePath"></param>
    /// <param name="httpMethod"></param>
    /// <param name="apiMethod"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public Task<T> GetResponse<T>(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, HttpContent content);

    /// <summary>
    /// Send a Http request and return the response as the specified type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="baseUrl"></param>
    /// <param name="relativePath"></param>
    /// <param name="httpMethod"></param>
    /// <param name="apiMethod"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public Task<T> GetResponse<T>(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, object content);

    /// <summary>
    /// Send a Http request and return the response as a stream,
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="relativePath"></param>
    /// <param name="httpMethod"></param>
    /// <param name="apiMethod"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public Task<System.IO.Stream> GetResponseAsStream(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, HttpContent content);

    /// <summary>
    /// Send a Http request and return the response as a stream,
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="relativePath"></param>
    /// <param name="httpMethod"></param>
    /// <param name="apiMethod"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public Task<System.IO.Stream> GetResponseAsStream(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, object content);

    /// <summary>
    /// Send a Http request and return the response as a string.
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="relativePath"></param>
    /// <param name="httpMethod"></param>
    /// <param name="apiMethod"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public Task<string> GetResponseAsString(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, HttpContent content);

    /// <summary>
    /// Send a Http request and return the response as a string.
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="relativePath"></param>
    /// <param name="httpMethod"></param>
    /// <param name="apiMethod"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public Task<string> GetResponseAsString(string baseUrl, string relativePath, HttpMethod httpMethod, string apiMethod, object content);
  }
}
