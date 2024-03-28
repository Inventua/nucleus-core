using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;
using Nucleus.Data.Common;
using Nucleus.Modules.PageLinks.DataProviders;
using Nucleus.Modules.PageLinks.Models;

namespace Nucleus.Modules.PageLinks;
public class PageLinksManager
{
  private IDataProviderFactory DataProviderFactory { get; }
  private ICacheManager CacheManager { get; }

  public PageLinksManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
  {
    this.CacheManager = cacheManager;
    this.DataProviderFactory = dataProviderFactory;
  }

  /// <summary>
	/// Create a new <see cref="PageLinks"/> with default values.
  /// </summary>
  /// <returns></returns>
  public Task<PageLink> CreateNew()
  {
    PageLink result = new();

    return Task.FromResult(result);
  }

  /// <summary>
  /// List all <see cref="PageLink"/>s within the specified site.
  /// </summary>
  /// <param name="module"></param>
  /// <returns></returns>
  public async Task<List<PageLink>> List(PageModule module)
  {
    return await this.CacheManager.ModulePageLinksCache().GetAsync(module.Id, async id =>
    {
      using (IPageLinksDataProvider provider = this.DataProviderFactory.CreateProvider<IPageLinksDataProvider>())
      {
        return await provider.List(module);
      }
    });
  }

  /// <summary>
  /// Update the <see cref="Models.PageLink.SortOrder"/> of the page link specifed by id by swapping it with the next-highest <see cref="Models.PageLink.SortOrder"/>.
  /// </summary>
  /// <param name="id"></param>
  public async Task MoveDown(PageModule module, Guid id)
  {
    PageLink previousLink = null;

    List<PageLink> pageLinks = await this.List(module);
    pageLinks.Reverse();

    using (IPageLinksDataProvider provider = this.DataProviderFactory.CreateProvider<IPageLinksDataProvider>())
    {
      foreach (PageLink pageLink in pageLinks)
      {
        if (pageLink.Id == id)
        {
          if (previousLink != null)
          {
            int temp = pageLink.SortOrder;
            pageLink.SortOrder = previousLink.SortOrder;
            previousLink.SortOrder = temp;

            await provider.Save(module, previousLink);
            await provider.Save(module, pageLink);

            this.CacheManager.ModulePageLinksCache().Remove(module.Id);
            break;
          }
        }
        else
        {
          previousLink = pageLink;
        }
      }
    }
  }

  /// <summary>
  /// Update the <see cref="Models.PageLink.SortOrder"/> of the link specifed by id by swapping it with the previous <see cref="Models.PageLink.SortOrder"/>.
  /// </summary>
  /// <param name="id"></param>
  public async Task MoveUp(PageModule module, Guid id)
  {
    PageLink previousLink = null;

    List<PageLink> pageLinks = await this.List(module);
    using (IPageLinksDataProvider provider = this.DataProviderFactory.CreateProvider<IPageLinksDataProvider>())
    {

      foreach (PageLink pageLink in pageLinks)
      {
        if (pageLink.Id == id)
        {
          if (previousLink != null)
          {
            int temp = pageLink.SortOrder;
            pageLink.SortOrder = previousLink.SortOrder;
            previousLink.SortOrder = temp;

            await provider.Save(module, previousLink);
            await provider.Save(module, pageLink);

            this.CacheManager.ModulePageLinksCache().Remove(module.Id);

            break;
          }
        }
        else
        {
          previousLink = pageLink;
        }
      }
    }
  }

  /// <summary>
  /// Create or update a <see cref="PageLink"/>.
  /// </summary>
  /// <param name="module"></param>
  /// <param name="pageLink"></param>
  public async Task Save(PageModule module, List<PageLink> pageLinks)
  {
    List<PageLink> originalPageLinks = await this.List(module);

    foreach (PageLink deleted in originalPageLinks.ExceptBy(pageLinks.Select(link => link.Id), pageLink => pageLink.Id))
    {
      using (IPageLinksDataProvider provider = this.DataProviderFactory.CreateProvider<IPageLinksDataProvider>())
      {
        await provider.Delete(deleted);
      }
    }

    foreach (PageLink pageLink in pageLinks)
    {
      using (IPageLinksDataProvider provider = this.DataProviderFactory.CreateProvider<IPageLinksDataProvider>())
      {
        await provider.Save(module, pageLink);
      }
    }

    this.CacheManager.ModulePageLinksCache().Remove(module.Id);
  }
}
