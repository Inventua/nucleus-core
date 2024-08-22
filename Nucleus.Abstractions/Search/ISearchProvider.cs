using System.Threading.Tasks;

namespace Nucleus.Abstractions.Search;

/// <summary>
/// Defines the interface for a search provider.
/// </summary>
/// <remarks>
/// Search providers retrieve search results from a search engine.  The interface for search index management is provided by 
/// a separate interface, <see cref="ISearchIndexManager"/>.
/// </remarks>	
public interface ISearchProvider
{
  /// <summary>
  /// Return results matching the values specified by <paramref name="query"/>.
  /// </summary>
  /// <param name="query"></param>
  /// <returns></returns>
  public Task<SearchResults> Search(SearchQuery query);

  /// <summary>
  /// Return search suggestions matching the values specified by <paramref name="query"/>.
  /// </summary>
  /// <param name="query"></param>
  /// <returns></returns>
  /// <remarks>
  /// Implementations of this interface which do not provide search suggestions can throw a NotImplementedException.
  /// </remarks>
  public Task<SearchResults> Suggest(SearchQuery query);

  /// <summary>
  /// Return a <see cref="ISearchProviderCapabilities" /> object which reports the search provider's cababilities and limitations.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// This method has a default implementation, which returns a <see cref="ISearchProviderCapabilities" /> with no restrictions. Therefore, Search 
  /// providers which support all features do not need to implement this method.
  /// </remarks>
  public ISearchProviderCapabilities GetCapabilities()
  {
    return new DefaultSearchProviderCapabilities();
  }
}
