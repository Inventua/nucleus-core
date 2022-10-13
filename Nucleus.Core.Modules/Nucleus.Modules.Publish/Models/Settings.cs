using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Publish.Models
{
  public class Settings 
  {
    private const string MODULESETTING_CATEGORYLIST_ID = "articles:categorylistid";

    public Guid CategoryListId { get; set; }
    public LayoutOptions LayoutOptions { get; set; } = new();


    public void GetSettings(PageModule module)
    {
      this.CategoryListId = module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty);
      // layout settings
      this.LayoutOptions.GetSettings(module);
    }

    public void SetSettings(PageModule module)
    {
      module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, this.CategoryListId);
      this.LayoutOptions.SetSettings(module);
    }
  }
}
