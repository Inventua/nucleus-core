using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish.Models
{
  public class LayoutOptions : ModelBase
  {
    private const string MODULESETTING_VIEWER_LAYOUT = "publish:viewer-layout";
    private const string MODULESETTING_MASTER_LAYOUT = "publish:master-layout";
    private const string MODULESETTING_PRIMARY_ARTICLE_LAYOUT = "publish:primary-article-layout";
    private const string MODULESETTING_PRIMARY_ARTICLE_COUNT = "publish:primary-article-count";
    private const string MODULESETTING_PRIMARY_FEATURED_ONLY = "publish:primary-featured-only";
    private const string MODULESETTING_SECONDARY_ARTICLE_LAYOUT = "publish:secondary-article-layout";
    private const string MODULESETTING_SECONDARY_ARTICLE_COUNT = "publish:secondary-article-count";
    //private const string MODULESETTING_SECONDARY_FEATURED_ONLY = "publish:secondary-featured-only";


    public IEnumerable<string> ViewerLayouts { get; set; }
    public IEnumerable<string> MasterLayouts { get; set; }
    public IEnumerable<string> ArticleLayouts { get; set; }

    public string ViewerLayout { get; set; } // eg front page, table, tile
    public string MasterLayout { get; set; }
    public string PrimaryArticleLayout { get; set; }
    public int PrimaryArticleCount { get; set; }
    public Boolean PrimaryFeaturedOnly { get; set; }
    public string SecondaryArticleLayout { get; set; }
    public int SecondaryArticleCount { get; set; }
    //public Boolean SecondaryFeaturedOnly { get; set; }

    public void GetSettings(PageModule module)
    {

      // layout settings
      this.ViewerLayout = module.ModuleSettings.Get(MODULESETTING_VIEWER_LAYOUT, "Table");
      this.MasterLayout = module.ModuleSettings.Get(MODULESETTING_MASTER_LAYOUT, "Table");
      this.PrimaryArticleLayout = module.ModuleSettings.Get(MODULESETTING_PRIMARY_ARTICLE_LAYOUT, "");
      this.PrimaryArticleCount = module.ModuleSettings.Get(MODULESETTING_PRIMARY_ARTICLE_COUNT, 0);
      this.PrimaryFeaturedOnly = module.ModuleSettings.Get(MODULESETTING_PRIMARY_FEATURED_ONLY, false);
      this.SecondaryArticleLayout = module.ModuleSettings.Get(MODULESETTING_SECONDARY_ARTICLE_LAYOUT, "");
      this.SecondaryArticleCount = module.ModuleSettings.Get(MODULESETTING_SECONDARY_ARTICLE_COUNT, 0);
    }

    public void SetSettings(PageModule module)
    {
      module.ModuleSettings.Set(MODULESETTING_VIEWER_LAYOUT, this.ViewerLayout);
      module.ModuleSettings.Set(MODULESETTING_MASTER_LAYOUT, this.MasterLayout);
      module.ModuleSettings.Set(MODULESETTING_PRIMARY_ARTICLE_LAYOUT, this.PrimaryArticleLayout);
      module.ModuleSettings.Set(MODULESETTING_PRIMARY_ARTICLE_COUNT, this.PrimaryArticleCount);
      module.ModuleSettings.Set(MODULESETTING_PRIMARY_FEATURED_ONLY, this.PrimaryFeaturedOnly);
      module.ModuleSettings.Set(MODULESETTING_SECONDARY_ARTICLE_LAYOUT, this.SecondaryArticleLayout);
      module.ModuleSettings.Set(MODULESETTING_SECONDARY_ARTICLE_COUNT, this.SecondaryArticleCount);
    }

  }
}
