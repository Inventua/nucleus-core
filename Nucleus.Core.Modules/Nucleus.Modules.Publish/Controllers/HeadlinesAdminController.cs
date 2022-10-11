using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Modules.Publish.Models;

namespace Nucleus.Modules.Publish.Controllers
{
  [Extension("Publish")]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  public class HeadlinesAdminController : Controller
  {
    private Context Context { get; }
    private IPageManager PageManager { get; }
    private IPageModuleManager PageModuleManager { get; }
    private IListManager ListManager { get; }
    private IExtensionManager ExtensionManager { get; }
    private HeadlinesManager HeadlinesManager { get; }
    private string ViewerLayoutFolder { get; }

    public HeadlinesAdminController(IWebHostEnvironment webHostEnvironment, Context Context, HeadlinesManager headlinesManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IListManager listManager, IExtensionManager extensionManager)
    {
      this.Context = Context;
      this.HeadlinesManager = headlinesManager;
      this.PageManager = pageManager;
      this.PageModuleManager = pageModuleManager;
      this.ListManager = listManager;
      this.ExtensionManager = extensionManager;

      this.ViewerLayoutFolder = $"{webHostEnvironment.ContentRootPath}\\{Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER}\\Publish\\Views\\ViewerLayouts\\";
    }

    public async Task<ActionResult> Settings()
    {
      return View("HeadlinesSettings", await BuildSettingsViewModel(null));
    }

    [HttpGet]
    [HttpPost]
    public async Task<ActionResult> Settings(ViewModels.HeadlinesSettings viewModel)
    {
      return View("HeadlinesSettings", await BuildSettingsViewModel(viewModel));
    }

    [HttpPost]
    public async Task<ActionResult> SaveSettings(ViewModels.HeadlinesSettings viewModel)
    {
      Models.FilterOptions filterOptions = new();
     
      foreach(ViewModels.HeadlinesCategorySelection categorySelection in viewModel.Categories.Where(selection => selection.IsSelected))
      {
        filterOptions.Categories.Add(categorySelection.Category);
      }

      await this.HeadlinesManager.SaveFilterOptions(this.Context.Module, filterOptions);

      viewModel.SetSettings(this.Context.Module);
      await this.PageModuleManager.SaveSettings(this.Context.Module);

      return Ok();
    }

    #region "    Private methods    "
    private async Task<String> BuildInstanceCaption(PageModule pageModule)
    {
      Page page = await this.PageManager.Get(pageModule.PageId);
      return $"{page.Name} - {(String.IsNullOrEmpty(pageModule.Title) ? "Untitled Publish Module" : pageModule.Title)}";
    }

    private async Task<ViewModels.HeadlinesSettings> BuildSettingsViewModel(ViewModels.HeadlinesSettings viewModel)
    {
      if (viewModel == null)
      {
        viewModel = new();
        viewModel.GetSettings(this.Context.Module);
      }

      // Populate the module instances list
      IEnumerable<PageModule> modules = await this.ExtensionManager.ListPageModules(new() { Id = Guid.Parse("20af00b8-1d72-4c94-bce7-b175e0b173af") });
      foreach (PageModule moduleInstance in modules)
      {
        viewModel.ModuleInstances.Add(new ViewModels.HeadlinesSettings.ModuleInstance()
        {
          ModuleId = moduleInstance.Id,
          Caption = await BuildInstanceCaption(moduleInstance)
        });
      }

      // Only populate the category list if module instance has been selected
      if (viewModel.LinkedModuleId != Guid.Empty)
      {
        Abstractions.Models.PageModule linkedModule = await this.PageModuleManager.Get(viewModel.LinkedModuleId);

        Nucleus.Modules.Publish.ViewModels.Settings linkedModuleSettings = new ViewModels.Settings();
        linkedModuleSettings.GetSettings(linkedModule);

        await GetCategories(linkedModuleSettings, viewModel, await this.HeadlinesManager.GetFilterOptions(this.Context.Module));
      }

      ListLayouts(viewModel); 

      return viewModel;
    }

    private async Task GetCategories(ViewModels.Settings settings, ViewModels.HeadlinesSettings viewModel, Models.FilterOptions filterOptions)
    {
      if (settings.CategoryListId != Guid.Empty)
      {
        List categoryList = await this.ListManager.Get(settings.CategoryListId);

        if (categoryList != null)
        {
          viewModel.Categories.Clear();

          foreach (var item in categoryList.Items)
          {
            ViewModels.HeadlinesCategorySelection selection = new();

            // Selections for the headline category filter
            selection.Category = filterOptions.Categories.Where(category => category.Id == item.Id).FirstOrDefault();
            if (selection.Category != null)
            {
              selection.IsSelected = true;
            }
            else
            {
              selection.Category = item;
              selection.IsSelected = false;
            }
             
            viewModel.Categories.Add(selection);
          }
        }
      }
    }

    private void ListLayouts(ViewModels.HeadlinesSettings viewModel)
    {
      viewModel.LayoutOptions.ViewerLayouts = viewModel.LayoutOptions.ListViewerLayouts(this.ViewerLayoutFolder);

      if (String.IsNullOrEmpty(viewModel.LayoutOptions.ViewerLayout))
      {
        viewModel.LayoutOptions.ViewerLayout = viewModel.LayoutOptions.ViewerLayouts.FirstOrDefault();
      }

      viewModel.LayoutOptions.MasterLayouts = viewModel.LayoutOptions.ListMasterLayouts(this.ViewerLayoutFolder);
      viewModel.LayoutOptions.ArticleLayouts = viewModel.LayoutOptions.ListArticleLayouts(this.ViewerLayoutFolder);
    }
    #endregion
  }
}
