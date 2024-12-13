using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Maps.MapProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nucleus.Modules.Maps.Controllers;

[Extension("Maps")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class MapsSettingsController : Controller
{
  private IWebHostEnvironment WebHostEnvironment { get; }
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IHttpClientFactory HttpClientFactory { get; }

  private const string VIEWER_DIRECTORY = "Extensions/Maps/Views/MapViews/";

  public MapsSettingsController(IWebHostEnvironment webHostEnvironment, Context Context, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, IHttpClientFactory httpClientFactory)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
    this.FileSystemManager = fileSystemManager;
    this.HttpClientFactory = httpClientFactory;
  }

  [HttpGet]
  public async Task<ActionResult> Settings()
  {
    return View("Settings", await BuildSettingsViewModel(null));
  }

  [HttpPost]
  public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
  {
    ModelState.Clear();

    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> LookupAddress(ViewModels.Settings viewModel)
  {
    ModelState.Clear();

    IMapProvider mapProvider = MapsViewerController.GetMapProvider(viewModel.MapProvider);

    Models.Settings settings = mapProvider.GetSettings();
    settings.GetSettings(this.Context.Module);

    viewModel.MapSettings = settings;

    string key = viewModel.MapSettings.GetApiKey(this.Context.Site);

    Models.GeocodingLocation location = await mapProvider.GetGeocoder().LookupAddress(this.Context.Site, this.HttpClientFactory, settings, viewModel.Address);

    viewModel.MapSettings.Latitude = location.Geometry.Latitude;
    viewModel.MapSettings.Longitude = location.Geometry.Longitude;

    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel, [FromForm(Name=nameof(ViewModels.Settings.MapSettings))] Dictionary<string, string> settings)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    // set selected map provider.
    Models.Settings.SetMapProvider(this.Context.Module, viewModel.MapProvider);

    //Render map here and save the image in the selected folder
    if (viewModel.SelectedFolder != null && !string.IsNullOrEmpty(viewModel.MapFileName))
    {
      viewModel.MapSettings.MapFileId =  (await SaveRenderedMap(viewModel)).Id;
    }

    // set module properties
    viewModel.MapSettings.SetSettings(this.Context.Site, this.Context.Module, ViewModels.Settings.DUMMY_APIKEY);

    // set map provider-specific properties
    PopulateProviderSettings(viewModel.MapProvider, settings, viewModel.ApiKey);

    await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

    return Ok();
  }

  private void PopulateProviderSettings(string mapProviderName, Dictionary<string, string> values, string apiKey)
  {
    IMapProvider mapProvider = MapsViewerController.GetMapProvider(mapProviderName);

    Models.Settings existingSettings = mapProvider.GetSettings();
    existingSettings.GetSettings(this.Context.Module);

    foreach (KeyValuePair<string, string> value in values)
    {
      System.Reflection.PropertyInfo prop = existingSettings.GetType().GetProperty(value.Key);
      if (prop != null)
      {
        if (prop.PropertyType.IsEnum)
        {
          prop.SetValue(existingSettings, Enum.Parse(prop.PropertyType, value.Value));
        }
        else
        {
          prop.SetValue(existingSettings, Convert.ChangeType(value.Value, prop.PropertyType));
        }
      }
    }

    existingSettings.SetSettings(this.Context.Site, this.Context.Module, apiKey);
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel ??= new();
    }

    // Get the provider settings 
    if (viewModel.MapSettings == null || viewModel?.MapSettings.GetType() == typeof(Models.Settings))
    {
      viewModel.MapProvider = String.IsNullOrEmpty(viewModel.MapProvider) ? Models.Settings.GetMapProvider(this.Context.Module) : viewModel.MapProvider;

      viewModel.MapSettings = MapsViewerController.GetMapProvider(viewModel.MapProvider).GetSettings();
      viewModel.MapSettings.GetSettings(this.Context.Module);
    }

    viewModel.MapProviders = new();
    foreach (IFileInfo file in this.WebHostEnvironment.ContentRootFileProvider.GetDirectoryContents(VIEWER_DIRECTORY)
      .Where(fileInfo => System.IO.Path.GetExtension(fileInfo.Name).Equals(".cshtml", StringComparison.OrdinalIgnoreCase))
      .OrderBy(fileInfo => fileInfo.Name))
    {
      viewModel.MapProviders.Add(System.IO.Path.GetFileNameWithoutExtension(file.Name));
    }

    viewModel.MapProviderSettingsView = $"MapSettings/_{viewModel.MapProvider}Settings.cshtml";

    if (viewModel.SelectedFolder == null)
    {
      try
      {
        viewModel.SelectedFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.MapSettings.MapFileId);
        viewModel.MapFileName = viewModel.SelectedFile?.Name;
        viewModel.SelectedFolder = viewModel.SelectedFile?.Parent;
      }
      catch (Exception)
      {
        viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, this.FileSystemManager.ListProviders().First()?.Key, "");
      }
    }

    return viewModel;
  }

  private async Task<Abstractions.Models.FileSystem.File> SaveRenderedMap(ViewModels.Settings viewModel)
  {
    IMapProvider mapProvider = MapsViewerController.GetMapProvider(viewModel.MapProvider);

    Models.Settings settings = mapProvider.GetSettings();

    settings.GetSettings(this.Context.Module);

    viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Id);
    
    using (System.IO.Stream mapFileStream = await mapProvider.GetRenderer().RenderMap(this.Context.Site, this.HttpClientFactory, settings))
    {
      mapFileStream.Position = 0;
      return await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedFolder.Provider, viewModel.SelectedFolder.Path, viewModel.MapFileName, mapFileStream, true);
    }
  }

}