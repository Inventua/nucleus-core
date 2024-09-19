using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions.BingCustomSearch.Models;
using System.Text.Json;

namespace Nucleus.Extensions.BingCustomSearch;

internal static class BingApi
{
  internal static async Task<BingCustomSearchResponse> GetBingSearchResponse(IHttpClientFactory HttpClientFactory, SearchQuery query, Models.Settings settings)
  {
    HttpClient httpClient = HttpClientFactory.CreateClient();

    string url = $"{Settings.SEARCH_BASE_URI}?q={query.SearchTerm}&customconfig={settings.ConfigurationId}&count={query.PagingSettings.PageSize}&offset={query.PagingSettings.FirstRowIndex}&safeSearch={settings.SafeSearch}&textDecorations=true&textFormat=HTML";

    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
    requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", settings.GetApiKey(query.Site));

    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

    if (!response.IsSuccessStatusCode)
    {
      BingCustomSearchErrorResponse errorResponse = await JsonSerializer.DeserializeAsync<BingCustomSearchErrorResponse>(await response.Content.ReadAsStreamAsync());
      throw new InvalidOperationException(String.Join("\n", errorResponse.Errors.Select(error => error.Message)));
    }

    BingCustomSearchResponse searchResponse = await JsonSerializer.DeserializeAsync<BingCustomSearchResponse>(await response.Content.ReadAsStreamAsync());

    return searchResponse;
  }

  internal static async Task<BingCustomSuggestions> GetBingSuggestions(IHttpClientFactory HttpClientFactory, SearchQuery query, Models.Settings settings)
  {
    HttpClient httpClient = HttpClientFactory.CreateClient();

    string url = $"{Settings.SUGGEST_BASE_URI}?q={query.SearchTerm}&customconfig={settings.ConfigurationId}&safeSearch={settings.SafeSearch}&textDecorations=true&textFormat=HTML";

    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
    requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", settings.GetApiKey(query.Site));

    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

    if (!response.IsSuccessStatusCode)
    {
      BingCustomSearchErrorResponse errorResponse = await JsonSerializer.DeserializeAsync<BingCustomSearchErrorResponse>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { });

      if (errorResponse.Errors != null)
      {
        throw new InvalidOperationException(String.Join("\n", errorResponse.Errors.Select(error => error.Message)));
      }

      response.EnsureSuccessStatusCode();
    }

    BingCustomSuggestions suggestions = await JsonSerializer.DeserializeAsync<BingCustomSuggestions>(await response.Content.ReadAsStreamAsync());

    return suggestions;
  }
}

