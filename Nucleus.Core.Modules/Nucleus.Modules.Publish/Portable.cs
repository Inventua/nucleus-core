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

  public Task<List<object>> Export(PageModule module)
  {
    throw new NotImplementedException();
  }

  public async Task Import(PageModule module, List<object> items)
  {
    Abstractions.Models.Page page = await this.PageManager.Get(module);
    Abstractions.Models.Site site = await this.SiteManager.Get(page);
      
    foreach (Models.Article article in items.Select(item => item.CopyTo<Models.Article>())) 
    {
      Models.Article existing = (await this.ArticlesManager.List(module))
        .Where(doc=>doc.Title.Equals(article.Title, StringComparison.OrdinalIgnoreCase))
        .FirstOrDefault();

      if (existing != null)
      {
        article.Id = existing.Id; 
      }

      await this.ArticlesManager.Save(module, article);
    }
  }
}
