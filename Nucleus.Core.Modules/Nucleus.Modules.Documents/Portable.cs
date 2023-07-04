using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Portable;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Modules.Documents;

public class Portable : Nucleus.Abstractions.Portable.IPortable
{
  private ISiteManager SiteManager { get; }
  private IPageManager PageManager { get; }
  private DocumentsManager DocumentsManager { get; }

  public Portable(ISiteManager siteManager, IPageManager pageManager, DocumentsManager documentsManager)
  {
    this.SiteManager = siteManager;
    this.PageManager = pageManager;
    this.DocumentsManager = documentsManager;
  }

  public Guid ModuleDefinitionId => new Guid("28df7ff3-6407-459e-8608-c1ef4181807c");

  public string Name => "Documents";

  public Task<List<object>> Export(PageModule module)
  {
    throw new NotImplementedException();
  }

  public async Task Import(PageModule module, List<object> items)
  {
    Abstractions.Models.Page page = await this.PageManager.Get(module);
    Abstractions.Models.Site site = await this.SiteManager.Get(page);
      
    foreach (Models.Document document in items.Select(item => item.CopyTo<Models.Document>())) 
    {
      Models.Document existing = (await this.DocumentsManager.List(site, module))
        .Where(doc=>doc.Title.Equals(document.Title, StringComparison.OrdinalIgnoreCase))
        .FirstOrDefault();

      if (existing != null)
      {
        document.Id = existing.Id; 
      }

      await this.DocumentsManager.Save(module, document);
    }
  }
}
