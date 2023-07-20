using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Portable;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Modules.Links;

public class Portable : Nucleus.Abstractions.Portable.IPortable
{
  private ISiteManager SiteManager { get; }
  private IPageManager PageManager { get; }
  private LinksManager LinksManager { get; }

  public Portable(ISiteManager siteManager, IPageManager pageManager, LinksManager linksManager)
  {
    this.SiteManager = siteManager;
    this.PageManager = pageManager;
    this.LinksManager = linksManager;
  }

  public Guid ModuleDefinitionId => new Guid("374e62b5-024d-4d8d-95a2-e56f476fe887");

  public string Name => "Links";

  public Task<Nucleus.Abstractions.Portable.PortableContent> Export(PageModule module)
  {
    throw new NotImplementedException();
  }

  public async Task Import(PageModule module, Nucleus.Abstractions.Portable.PortableContent content)
  {
    Abstractions.Models.Page page = await this.PageManager.Get(module);
    Abstractions.Models.Site site = await this.SiteManager.Get(page);

    if (content.TypeURN.Equals(Models.Link.URN, StringComparison.OrdinalIgnoreCase))
    {
      foreach (Models.Link link in content.Items.Select(item => item.CopyTo<Models.Link>()))
      {
        Models.Link existing = (await this.LinksManager.List(site, module))
          .Where(doc => doc.Title.Equals(link.Title, StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault();

        if (existing != null)
        {
          link.Id = existing.Id;
        }

        await this.LinksManager.Save(module, link);
      }
    }
    else
    {
      throw new InvalidOperationException($"Type URN '{content.TypeURN}' is not recognized by the Links module.");
    }
  }
}
