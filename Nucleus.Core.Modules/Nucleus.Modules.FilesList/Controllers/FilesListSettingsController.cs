using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Modules.FilesList.Controllers;

[Extension("FilesList")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class FilesListSettingsController : Controller
{
  private IWebHostEnvironment WebHostEnvironment { get; }
  private Context Context { get; }
  private FolderOptions FolderOptions { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IFileSystemManager FileSystemManager { get; }

  public FilesListSettingsController(IWebHostEnvironment webHostEnvironment, Context Context, IOptions<FolderOptions> folderOptions, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.Context = Context;
    this.FolderOptions = folderOptions.Value;
    this.PageModuleManager = pageModuleManager;
    this.FileSystemManager = fileSystemManager;
  }

  [HttpGet]
  [HttpPost]
  public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
  {
    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
  {
    if (viewModel.SelectedFolder != null)
    {
      viewModel.SourceFolderId = viewModel.SelectedFolder.Id;
    }

    viewModel.SetSettings(this.Context.Module);

    await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

    return Ok();
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
    }

    viewModel.GetSettings(this.Context.Module);

    if (viewModel.SelectedFolder == null)
    {
      try
      {
        viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SourceFolderId);
        viewModel.SourceFolderId = viewModel.SelectedFolder.Id;
      }
      catch (Exception)
      {
        viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, this.FileSystemManager.ListProviders().First()?.Key, "");
      }
    }

    viewModel.Layouts = new();

    // List viewer layouts.  These can be embedded, or shipped in the install set.  The special NucleusModuleEmbedViewerLayouts target in
    // module.build.targets embeds the files (they are also compiled at build time).
    foreach (IFileInfo file in this.WebHostEnvironment.ContentRootFileProvider.GetDirectoryContents("Extensions/FilesList/Views/ViewerLayouts/")
      .Where(fileInfo => System.IO.Path.GetExtension(fileInfo.Name).Equals(".cshtml", StringComparison.OrdinalIgnoreCase))
      .OrderBy(fileInfo => fileInfo.Name))
    {
      viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file.Name));
    }
    return viewModel;
  }
}