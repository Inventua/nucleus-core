using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
using Nucleus.Modules.Links.Models;

namespace Nucleus.Modules.Links.Controllers;

[Extension("Links")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class AdminController : Controller
{
  public const string MODULESETTING_CATEGORYLIST_ID = "links:categorylistid";
  public const string MODULESETTING_LAYOUT = "links:layout";
  public const string MODULESETTING_OPEN_NEW_WINDOW = "links:opennewwindow";
  public const string MODULESETTING_SHOW_IMAGES = "links:showimages";

  private IWebHostEnvironment WebHostEnvironment { get; }
  private Context Context { get; }
  private IPageManager PageManager { get; }
  private IPageModuleManager PageModuleManager { get; }
  private LinksManager LinksManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IListManager ListManager { get; }
  private FolderOptions FolderOptions { get; }

  public AdminController(IWebHostEnvironment webHostEnvironment, Context Context, IOptions<FolderOptions> folderOptions, IPageManager pageManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, LinksManager linksManager, IListManager listManager)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.Context = Context;
    this.FolderOptions = folderOptions.Value;
    this.PageManager = pageManager;
    this.PageModuleManager = pageModuleManager;
    this.FileSystemManager = fileSystemManager;
    this.LinksManager = linksManager;
    this.ListManager = listManager;
  }

  [HttpGet]
  [HttpPost]
  public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
  {
    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> Create()
  {
    return View("Editor", await BuildEditorViewModel(null, Guid.Empty, false));
  }

  [HttpGet]
  [HttpPost]
  public async Task<ActionResult> Editor(ViewModels.Editor viewModel, Guid id, string mode)
  {
    return View("Editor", await BuildEditorViewModel(viewModel, id, mode?.Equals("standalone", StringComparison.OrdinalIgnoreCase) == true));
  }

  [HttpGet]
  [HttpPost]
  public async Task<ActionResult> DeleteLink(ViewModels.Settings viewModel, Guid id)
  {
    Link link = await this.LinksManager.Get(this.Context.Site, id);
    await this.LinksManager.Delete(link);
    return View("_LinksList", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SelectAnother(ViewModels.Editor viewModel)
  {
    viewModel.Link.LinkFile.File.ClearSelection();

    return View("Editor", await BuildEditorViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SelectAnotherImage(ViewModels.Editor viewModel)
  {
    viewModel.Link.ImageFile.ClearSelection();

    return View("Editor", await BuildEditorViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> MoveUp(ViewModels.Settings viewModel, Guid id)
  {
    await this.LinksManager.MoveUp(this.Context.Module, id);
    return View("_LinksList", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> MoveDown(ViewModels.Settings viewModel, Guid id)
  {
    await this.LinksManager.MoveDown(this.Context.Module, id);
    return View("_LinksList", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> UploadImageFile(ViewModels.Editor viewModel, [FromForm] IFormFile mediaFile)
  {
    if (mediaFile != null)
    {
      viewModel.Link.ImageFile.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Link.ImageFile.Parent.Id);
      using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
      {
        viewModel.Link.ImageFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.Link.ImageFile.Provider, viewModel.Link.ImageFile.Parent.Path, mediaFile.FileName, fileStream, false);
      }
    }
    else
    {
      return BadRequest();
    }

    return View("Editor", await BuildEditorViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> UploadFile(ViewModels.Editor viewModel, [FromForm] IFormFile mediaFile)
  {
    if (mediaFile != null)
    {
      viewModel.Link.LinkFile.File.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Link.LinkFile.File.Parent.Id);
      using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
      {
        viewModel.Link.LinkFile.File = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.Link.LinkFile.File.Provider, viewModel.Link.LinkFile.File.Parent.Path, mediaFile.FileName, fileStream, false);
      }
    }
    else
    {
      return BadRequest();
    }

    return View("Editor", await BuildEditorViewModel(viewModel));
  }

  [HttpGet]
  public async Task<ActionResult> GetChildPages(Guid id)
  {
    ViewModels.PageIndexPartial viewModel = new();

    viewModel.FromPage = await this.PageManager.Get(id);

    viewModel.Pages = await this.PageManager.GetAdminMenu
      (
        this.Context.Site,
        await this.PageManager.Get(id),
        ControllerContext.HttpContext.User,
        1,
        true,
        false,
        true
      );

    return View("_PageMenu", viewModel);
  }

  [HttpPost]
  public async Task<ActionResult> SaveLink(ViewModels.Editor viewModel)
  {
    // Category is not mandatory
    ModelState.Remove("Link.Category.Id");
    ModelState.Remove("Link.Category.Name");
    ModelState.Remove("Link.LinkPage.Page.Name");
    ModelState.Remove("Link.ImageFile.Id");

    if (ModelState.IsValid)
    {
      await this.LinksManager.Save(this.Context.Module, viewModel.Link);
    }
    else
    {
      return BadRequest(ModelState);
    }

    if (viewModel.UseLayout == "_PopupEditor")
    {
      return Ok();
    }
    else
    {
      return View("_LinksList", await BuildSettingsViewModel(null));
    }
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> UpdateTitle(Guid id, string value)
  {
    Models.Link link = await this.LinksManager.Get(this.Context.Site, id);

    if (link == null)
    {
      return BadRequest();
    }
    else
    {
      link.Title = value;
    }

    await this.LinksManager.Save(this.Context.Module, link);

    return Ok();
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> UpdateDescription(Guid id, string value)
  {
    Models.Link link = await this.LinksManager.Get(this.Context.Site, id);

    if (link == null)
    {
      return BadRequest();
    }
    else
    {
      link.Description = value;
    }

    await this.LinksManager.Save(this.Context.Module, link);

    return Ok();
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
  {
    this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
    this.Context.Module.ModuleSettings.Set(MODULESETTING_LAYOUT, viewModel.Layout);
    this.Context.Module.ModuleSettings.Set(MODULESETTING_OPEN_NEW_WINDOW, viewModel.NewWindow);
    this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_IMAGES, viewModel.ShowImages);

    await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

    return Json(new { Title = "Changes Saved", Message = "Your changes have been saved.", Icon = "alert" });
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
    }

    viewModel.CategoryList = await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
    viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");
    viewModel.NewWindow = this.Context.Module.ModuleSettings.Get(MODULESETTING_OPEN_NEW_WINDOW, false);
    viewModel.ShowImages = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_IMAGES, false);

    viewModel.Lists = await this.ListManager.List(this.Context.Site);

    viewModel.Links = await this.LinksManager.List(this.Context.Site, this.Context.Module);


    viewModel.Layouts = new();
    // List viewer layouts.  These can be embedded, or shipped in the install set.  The special NucleusModuleEmbedViewerLayouts target in
    // the .csproj embeds the files (they are also compiled at build time).
    foreach (IFileInfo file in this.WebHostEnvironment.ContentRootFileProvider.GetDirectoryContents("Extensions/Links/Views/ViewerLayouts/")
      .Where(fileInfo => System.IO.Path.GetExtension(fileInfo.Name).Equals(".cshtml", StringComparison.OrdinalIgnoreCase))
      .OrderBy(fileInfo => fileInfo.Name))
    {
      viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file.Name));
    }

    return viewModel;
  }

  private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor input)
  {
    ViewModels.Editor viewModel;

    if (input == null)
    {
      viewModel = new ViewModels.Editor();
    }
    else
    {
      viewModel = input;
    }

    await ReadEditorViewModel(viewModel, input.Link.Id);

    return viewModel;
  }

  private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor input, Guid id, Boolean standalone)
  {
    ViewModels.Editor viewModel;

    if (input == null)
    {
      viewModel = new ViewModels.Editor();
    }
    else
    {
      viewModel = input;
    }

    if (standalone)
    {
      viewModel.UseLayout = "_PopupEditor";
    }

    await ReadEditorViewModel(viewModel, id);

    return viewModel;
  }


  private async Task<ViewModels.Editor> ReadEditorViewModel(ViewModels.Editor viewModel, Guid id)
  {
    viewModel.CategoryList = await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));

    viewModel.LinkTypes = new();
    viewModel.LinkTypes.Add(Models.LinkTypes.Url, "Url");
    viewModel.LinkTypes.Add(Models.LinkTypes.Page, "Page");
    viewModel.LinkTypes.Add(Models.LinkTypes.File, "File");

    if (viewModel.Link == null)
    {
      if (id != Guid.Empty)
      {
        viewModel.Link = await this.LinksManager.Get(this.Context.Site, id);
      }
      else
      {
        viewModel.Link = await this.LinksManager.CreateNew();
      }
    }

    if (viewModel.Link?.ImageFile != null)
    {
      viewModel.Link.ImageFile = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.Link.ImageFile);
    }

    switch (viewModel.Link.LinkType)
    {
      case Models.LinkTypes.File:
        if (viewModel.Link.LinkFile != null)
        {
          viewModel.Link.LinkFile.File = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.Link.LinkFile?.File);
        }
        break;
      case Models.LinkTypes.Page:
        viewModel.PageMenu = (await this.PageManager.GetAdminMenu(this.Context.Site, null, ControllerContext.HttpContext.User, 1, true, false, true));
        break;
    }

    return viewModel;
  }

}