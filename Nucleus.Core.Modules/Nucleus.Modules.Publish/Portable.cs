using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Portable;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Modules.Publish;

public class Portable : Nucleus.Abstractions.Portable.IPortable
{
  private ISiteManager SiteManager { get; }
  private IPageManager PageManager { get; }
  private ArticlesManager ArticlesManager { get; }

  public Portable(ISiteManager siteManager, IPageManager pageManager, ArticlesManager articlesManager)
  {
    this.SiteManager = siteManager;
    this.PageManager = pageManager;
    this.ArticlesManager = articlesManager;
  }

  public Guid ModuleDefinitionId => new Guid("20af00b8-1d72-4c94-bce7-b175e0b173af");

  public string Name => "Publish";

  public Task<Nucleus.Abstractions.Portable.PortableContent> Export(PageModule module)
  {
    throw new NotImplementedException();
  }

  public async Task Import(PageModule module, Nucleus.Abstractions.Portable.PortableContent content)
  {
    Abstractions.Models.Page page = await this.PageManager.Get(module);
    Abstractions.Models.Site site = await this.SiteManager.Get(page);

    if (content.TypeURN.Equals(Models.Article.URN, StringComparison.OrdinalIgnoreCase))
    {
      foreach (Models.Article article in content.Items.Select(item => item.CopyTo<Models.Article>()))
      {
        Models.Article existing = (await this.ArticlesManager.List(module))
          .Where(doc => doc.Title.Equals(article.Title, StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault();

        if (existing != null)
        {
          article.Id = existing.Id;
        }

        await this.ArticlesManager.Save(module, article);
      }
    }
    else
    {
      throw new InvalidOperationException($"Type URN '{content.TypeURN}' is not recognized by the Publish module.");
    }
  }
}
