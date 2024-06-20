using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.Modules.Links.Models;

namespace Nucleus.Modules.Links.Controllers;

[Extension("Links")]
public class LinksController : Controller
{
  private IWebHostEnvironment WebHostEnvironment { get; }
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }
  private LinksManager LinksManager { get; }
  private IListManager ListManager { get; }
  private FolderOptions FolderOptions { get; }

  public LinksController(IWebHostEnvironment webHostEnvironment, Context Context, IOptions<FolderOptions> folderOptions, IPageModuleManager pageModuleManager, LinksManager linksManager, IListManager listManager)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.Context = Context;
    this.FolderOptions = folderOptions.Value;
    this.PageModuleManager = pageModuleManager;
    this.LinksManager = linksManager;
    this.ListManager = listManager;
  }

  [HttpGet]
  public async Task<ActionResult> Index()
  {
    return View("Viewer", await BuildViewModel());
  }

  private async Task<ViewModels.Viewer> BuildViewModel()
  {
    ViewModels.Viewer viewModel = new();

    viewModel.Links = new();
    foreach (Link link in await this.LinksManager.List(this.Context.Site, this.Context.Module))
    {
      Boolean isValid = true;

      // Check permissions
      switch (link.LinkType)
      {
        case LinkTypes.File:
          if (link.LinkFile.File.Id == Guid.Empty || !HttpContext.User.HasViewPermission(this.Context.Site, link.LinkFile.File.Parent))
          {
            isValid = false;
          }
          break;
        case LinkTypes.Page:
          if (link.LinkPage.Page.Disabled || !HttpContext.User.HasViewPermission(this.Context.Site, link.LinkPage.Page))
          {
            isValid = false;
          }
          break;
      }

      if (isValid)
      {
        viewModel.Links.Add(link);
      }
    }

    string layoutPath = $"ViewerLayouts/{this.Context.Module.ModuleSettings.Get(AdminController.MODULESETTING_LAYOUT, "Table")}.cshtml";

    //if (!System.IO.File.Exists($"{this.FolderOptions.GetExtensionFolder("Links", false)}//Views/{layoutPath}"))
    //{
    //	layoutPath = $"ViewerLayouts/Table.cshtml";
    //}
    // Check that the selected Viewer Layout file exists, if not, use the default (Table.cshtml)
    IFileInfo layoutFile = this.WebHostEnvironment.ContentRootFileProvider.GetFileInfo($"Extensions/Links/Views/{layoutPath}");
    if (!layoutFile.Exists)
    {
      layoutPath = $"ViewerLayouts/Table.cshtml";
    }

    viewModel.Layout = layoutPath;
    viewModel.CategoryList = await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(AdminController.MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
    viewModel.NewWindow = this.Context.Module.ModuleSettings.Get(AdminController.MODULESETTING_OPEN_NEW_WINDOW, false);
    viewModel.ShowImages = this.Context.Module.ModuleSettings.Get(AdminController.MODULESETTING_SHOW_IMAGES, false);

    return viewModel;
  }
}