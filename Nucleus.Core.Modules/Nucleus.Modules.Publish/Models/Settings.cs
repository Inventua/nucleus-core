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
    private const string MODULESETTING_LAYOUT = "articles:layout";

    public Guid CategoryListId { get; set; }
    public string Layout { get; set; }

    public void GetSettings(PageModule module)
    {
      this.CategoryListId = module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty);
      this.Layout = module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");
    }

    public void SetSettings(PageModule module)
    {
      module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, this.CategoryListId);
      module.ModuleSettings.Set(MODULESETTING_LAYOUT, this.Layout);
    }
  }
}
