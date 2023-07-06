using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Portable;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Modules.Forums;

public class Portable : Nucleus.Abstractions.Portable.IPortable
{
  private ISiteManager SiteManager { get; }
  private IPageManager PageManager { get; }
  private GroupsManager GroupsManager { get; }
  private ForumsManager ForumsManager { get; }

  public Portable(ISiteManager siteManager, IPageManager pageManager, GroupsManager groupsManager, ForumsManager forumsManager)
  {
    this.SiteManager = siteManager;
    this.PageManager = pageManager;
    this.GroupsManager = groupsManager;
    this.ForumsManager = forumsManager;
  }

  public Guid ModuleDefinitionId => new Guid("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

  public string Name => "Forums";

  public Task<List<object>> Export(PageModule module)
  {
    throw new NotImplementedException();
  }

  public async Task Import(PageModule module, List<object> items)
  {
    Abstractions.Models.Page page = await this.PageManager.Get(module);
    Abstractions.Models.Site site = await this.SiteManager.Get(page);
      
    foreach (Models.Group group in items.Select(item => item.CopyTo<Models.Group>())) 
    {
      Models.Group existingGroup = (await this.GroupsManager.List(module))
        .Where(existing => existing.Name.Equals(group.Name, StringComparison.OrdinalIgnoreCase))
        .FirstOrDefault();

      if (existingGroup != null)
      {
        group.Id = existingGroup.Id; 
      }

      await this.GroupsManager.Save(module, group);

      if (group.Forums != null)
      {
        foreach (Models.Forum forum in group.Forums)
      {
          Models.Forum existingForum = (await this.ForumsManager.List(group))
            .Where(existing => existing.Name.Equals(forum.Name, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

          if (existingForum != null)
          {
            forum.Id = existingForum.Id;
          }
          
          await this.ForumsManager.Save(group, forum);
        }
      }
    }
  }
}
