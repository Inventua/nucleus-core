## Saving Settings
Extensions can save settings in a variety of ways.

### Module Settings
Simple name/value pairs of module instance-specific settings can be saved in the Nucleus ==PageModuleSettings== table.  In your controller 
class, obtain a reference to the current module by including a [Context](/api-documentation/Nucleus.Abstractions.Models.Context/) 
parameter in your constructor.  You can access the current module from the ==Module== property of the ==Context== object.

In the settings-related actions of your controller class, save module settings by adding or updating values 
in the [PageModule.ModuleSettings](/api-documentation/Nucleus.Abstractions.Models.PageModule/#ModuleSettings) property by using 
[ModuleSettingsExtensions.Set](/api-documentation/Nucleus.Extensions.ModuleSettingsExtensions/#Set(List<ModuleSetting>,String,String)), then calling 
[IPageModuleManager.SaveSettings](/api-documentation/Nucleus.Abstractions.Managers.IPageModuleManager/#SaveSettings(PageModule)).  

You can retrieve your settings by calling the [ModuleSettingsExtensions.Get](/api-documentation/Nucleus.Extensions.ModuleSettingsExtensions/#Get(List<ModuleSetting>,String,String)) 
extension on the [PageModule.ModuleSettings](/api-documentation/Nucleus.Abstractions.Models.PageModule/#ModuleSettings) property.

> Each module setting consists of a ==SettingName== and a SettingValue.  Module setting values are limited to 512 characters and are 
stored in the database as a string.  The ModuleSettingsExtensions [Set](/api-documentation/Nucleus.Extensions.ModuleSettingsExtensions/#Set(List<ModuleSetting>,String,String)) 
and [Get](/api-documentation/Nucleus.Extensions.ModuleSettingsExtensions/#Get(List<ModuleSetting>,String,String)) methods automatically convert your settings to and from ==Boolean==, ==Double==, 
==Enum==, ==Guid==, ==DateTime== and ==int== types automatically.  By convention, setting names are in the form modulename:valuename.

> This example is from the SiteMap module.  Code which does not demonstrate saving or retrieving module settings has been removed 
for brevity.

```
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Nucleus.Modules.Sitemap.Controllers
{
  [Extension("SiteMap")]
  public class SitemapController : Controller
  {
    private Context Context { get; }
    private IPageModuleManager PageModuleManager { get; }

    private class ModuleSettingsKeys
    {
      public const string SETTINGS_MAXLEVELS = "sitemap:maxlevels";
      public const string SETTINGS_ROOTPAGE_TYPE = "sitemap:root-page-type";
      public const string SETTINGS_ROOTPAGE = "sitemap:root-page";
      public const string SETTINGS_SHOWDESCRIPTION = "sitemap:show-description";
    }

    public SitemapController(Context context, IPageModuleManager pageModuleManager)
    {
      this.Context = context;
      this.PageModuleManager = pageModuleManager;      
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> Save(ViewModels.Sitemap viewModel)
    {
      // ModuleSettings.Set is an extension in the Nucleus.Extensions namespace.  It adds or updates a setting.
      this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_MAXLEVELS, viewModel.MaxLevels);
      this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_ROOTPAGE_TYPE, viewModel.RootPageType);
      this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_ROOTPAGE, viewModel.RootPageId);
      this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_SHOWDESCRIPTION, viewModel.ShowDescription);

      await this.PageModuleManager.SaveSettings(this.Context.Module);
      
      return Ok();
    }

    private ViewModels.Sitemap BuildViewModel()
    {
      ViewModels.Sitemap viewModel = new ViewModels.Sitemap();

      if (this.Context.Module != null)
      {
        viewModel.MaxLevels = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_MAXLEVELS, 0);
        viewModel.RootPageType = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_ROOTPAGE_TYPE, Nucleus.Modules.Sitemap.RootPageTypes.SelectedPage);
        viewModel.RootPageId = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_ROOTPAGE, Guid.Empty);
        viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_SHOWDESCRIPTION, false);
      }

      return viewModel;
    }
  }
}
```

### Site Settings
If your extension has settings which apply to the ==Site== rather than to a specific module instance, you can store them in the 
==SiteSettings== table.  The process is similar to saving and retrieving values for module settings.

Obtain a reference to the current [Site](/api-documentation/Nucleus.Abstractions.Models.Site/) by including a 
[Context](/api-documentation/Nucleus.Abstractions.Models.Context/) parameter in your constructor.  You can access the current 
module from the ==Context.Site== property.

In the settings-related actions of your controller class, save site settings by adding or updating values 
in the [Site.SiteSettings](/api-documentation/Nucleus.Abstractions.Models.Site/#SiteSettings) property by using 
[SiteSettingsExtensions.TrySetValue](/api-documentation/Nucleus.Extensions.SiteSettingsExtensions/#TrySetValue(List<SiteSetting>,String,Nullable<Boolean>)), 
then calling [ISiteManager.Save](/api-documentation/Nucleus.Abstractions.Managers.ISiteManager/#Save(Site)).  

You can retrieve your settings by calling the [SiteSettingsExtensions.TryGetValue](/api-documentation/Nucleus.Extensions.SiteSettingsExtensions/) 
extension of the [Site.SiteSettings](/api-documentation/Nucleus.Abstractions.Models.Site/#SiteSettings) property.

> Each site setting consists of a ==SettingName== and a SettingValue.  Site setting values are limited to 1024 characters and are 
stored in the database as a string.  The .TrySet and .TryGet methods automatically convert your settings to and from ==Boolean==, ==Double==, 
==Guid== and ==int== types automatically.  By convention, setting names are in the form extensionname:valuename.  Because the scope of 
==Site Settings== is the entire site, take care to ensure that your setting name will be unique.

> This example is from the Google Analytics extension.  Code which does not demonstrate saving/retrieving site settings has been removed 
for brevity.
```
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.GoogleAnalytics.Controllers
{
  [Extension("GoogleAnalytics")]
  public class GoogleAnalyticsController : Controller
  {
    private Context Context { get; }
    private ISiteManager SiteManager { get; }

    internal const string SETTING_ANALYTICS_ID = "googleanalytics:id";

    public GoogleAnalyticsController(Context context, ISiteManager siteManager)
    {
      this.Context = context;
      this.SiteManager = siteManager;
    }

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    [HttpPost]
    public ActionResult SaveSettings(ViewModels.Settings viewModel)
    {
      this.Context.Site.SiteSettings.TrySetValue(SETTING_ANALYTICS_ID, viewModel.GoogleAnalyticsId);
      this.SiteManager.Save(this.Context.Site);

      return Ok();
    }

    private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
    {
      if (viewModel == null)
      {
        viewModel = new();
      }

      if (this.Context.Site.SiteSettings.TryGetValue(SETTING_ANALYTICS_ID, out string googleId))
      {
        viewModel.GoogleAnalyticsId = googleId;
      }

      return viewModel;
    }
  }
}
```

### Content
Modules which store Html, Markdown or text content should use the ==Content== table to store content.  Content records consist of a 
reference to the module which they belong to, a title, the content and a sort index.  You can store multiple content records for a single 
module instance in the ==Content== table. 

Obtain a reference to [IContentManager](/api-documentation/Nucleus.Abstractions.Managers.IContentManager/) by including a
parameter in your constructor.  The ==Content Manager== has methods to Save, List and manage content records.

> This example is from the Multi-Content module.  Code which does not demonstrate the content manager has been removed for brevity.
```
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.MultiContent.Controllers
{
  [Extension("MultiContent")]
  public class MultiContentController : Controller
  {
    private Context Context { get; }
    private IContentManager ContentManager { get; }

    public MultiContentController(Context Context, IContentManager contentManager)
    {
      this.Context = Context;
      this.ContentManager = contentManager;
    }

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpGet]
    [HttpPost]
    public async Task<ActionResult> Edit(ViewModels.Editor viewModel, Guid id)
    {
      return View("Editor", await BuildEditorViewModel(viewModel, id));
    }

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpPost]
    public async Task<ActionResult> SaveContent(ViewModels.Editor viewModel)
    {
      await this.ContentManager.Save(this.Context.Module, viewModel.Content);

      return View("_ContentList", await BuildSettingsViewModel(new ViewModels.Settings()));
    }

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpPost]
    public async Task<ActionResult> Delete(ViewModels.Settings viewModel, Guid id)
    {
      Content content = await this.ContentManager.Get(id);
      await this.ContentManager.Delete(content);

      return View("_ContentList", await BuildSettingsViewModel(viewModel));
    }

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpPost]
    public async Task<ActionResult> MoveUp(ViewModels.Settings viewModel, Guid id)
    {
      await this.ContentManager.MoveUp(this.Context.Module, id);
      return View("_ContentList", await BuildSettingsViewModel(viewModel));
    }

    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    [HttpPost]
    public async Task<ActionResult> MoveDown(ViewModels.Settings viewModel, Guid id)
    {
      await this.ContentManager.MoveDown(this.Context.Module, id);
      return View("_ContentList", await BuildSettingsViewModel(viewModel));
    }

    private async Task<ViewModels.Viewer> BuildViewModel()
    {
      ViewModels.Viewer viewModel = new();
      viewModel.Contents = await this.ContentManager.List(this.Context.Module);
      
      return viewModel;
    }

    private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor input, Guid contentId)
    {
      if (input.Content == null)
      {
        if (contentId == Guid.Empty)
        {
          input.Content = new();
        }
        else
        {
          input.Content = await this.ContentManager.Get(contentId);
        }
      }

      return input;
    }
  }
}
```


### Use you own database tables
Complex settings which aren't just name/value pairs should be stored in database tables that you create for your extension.