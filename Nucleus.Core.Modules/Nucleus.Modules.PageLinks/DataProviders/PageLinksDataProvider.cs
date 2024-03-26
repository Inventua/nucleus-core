using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Modules.PageLinks.Models;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;

namespace Nucleus.Modules.PageLinks.DataProviders;

/// <summary>
/// Links module data provider.
/// </summary>
/// <remarks>
/// This class implements the IPageLinksDataProvider interface, and inherits the base Nucleus entity framework data provider class.
/// </remarks>
public class PageLinksDataProvider : Nucleus.Data.EntityFramework.DataProvider, IPageLinksDataProvider
{
  protected IEventDispatcher EventManager { get; }
  protected new PageLinksDbContext Context { get; }

  public PageLinksDataProvider(PageLinksDbContext context, IEventDispatcher eventManager, ILogger<PageLinksDataProvider> logger) : base(context, logger)
  {
    this.EventManager = eventManager;
    this.Context = context;
  }
    
  public async Task<List<PageLink>> List(PageModule pageModule)
  {
    List<PageLink> results = await this.Context.PageLinks
      .Where(pageLink => EF.Property<Guid>(pageLink, "ModuleId") == pageModule.Id)
      .OrderBy(pageLink => pageLink.SortOrder)
      .AsSplitQuery()
      .AsNoTracking()
      .ToListAsync();

    return results;
  }

  public async Task Delete(PageLink pageLink)
  {
    this.Context.Remove(pageLink);
    await this.Context.SaveChangesAsync<PageLink>();
  }

  public async Task Save(PageModule pageModule, PageLink pageLink)
  {
    Action raiseEvent;

    Boolean isNew = !this.Context.PageLinks.Where(existing => existing.Id == pageLink.Id)
      .AsNoTracking()
      .Any();

    this.Context.Attach(pageLink);
    this.Context.Entry(pageLink).Property("ModuleId").CurrentValue = pageModule.Id;

    if (isNew)
    {
      pageLink.SortOrder = await GetTopLinkSortOrder(pageModule.Id) + 10;
      this.Context.Entry(pageLink).State = EntityState.Added;
      raiseEvent = new(() => { this.EventManager.RaiseEvent<PageLink, Create>(pageLink); });
    }
    else
    {
      this.Context.Entry(pageLink).State = EntityState.Modified;
      raiseEvent = new(() => { this.EventManager.RaiseEvent<PageLink, Update>(pageLink); });
    }

    await this.Context.SaveChangesAsync<PageLink>();

    raiseEvent.Invoke();
  }

  private async Task<int> GetTopLinkSortOrder(Guid moduleId)
  {
    PageLink pageLink = await this.Context.PageLinks
      .Where(pageLink => EF.Property<Guid>(pageLink, "ModuleId") == moduleId)
      .OrderByDescending(pageLink => pageLink.SortOrder)
      .AsNoTracking()
      .FirstOrDefaultAsync();

    return pageLink == null ? 10 : pageLink.SortOrder;
  }
}

