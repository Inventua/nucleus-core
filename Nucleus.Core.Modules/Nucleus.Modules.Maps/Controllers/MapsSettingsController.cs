using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Modules.Maps.MapGeocoders;
using Nucleus.Modules.Maps.MapProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.Modules.Maps.Controllers;

[Extension("Maps")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class MapsSettingsController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IEnumerable<MapProvider> MapProviders { get; }

  //private const string VIEWER_DIRECTORY = "Extensions/Maps/Views/MapViews/";

  public MapsSettingsController(Context Context, IEnumerable<MapProvider> mapProviders, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
  {
    this.MapProviders = mapProviders;
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
    this.FileSystemManager = fileSystemManager;
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

    MapProvider mapProvider = MapsViewerController.GetMapProvider(this.MapProviders, viewModel.MapProviderTypeName);

    Models.Settings settings = mapProvider.GetSettings(this.Context.Module);

    settings.SetApiKey (this.Context.Site, GetApiKey(viewModel, settings));

    IEnumerable<GeocodingLocation> locations = await mapProvider.Geocoder.LookupAddress(settings, viewModel.Address);

    if (!(locations?.Any() == true))
    {
      return this.PopupMessage("Map Lookup", "The address was not found.", ControllerExtensions.PopupIcons.Alert);
    }

    viewModel.MapSettings = settings;
    viewModel.Locations = locations;

    if (locations.First()?.Geometry != null)
    {
      viewModel.MapSettings.Latitude = locations.First().Geometry.Latitude;
      viewModel.MapSettings.Longitude = locations.First().Geometry.Longitude;
    }

    viewModel.ShowLocationPicker = viewModel.Locations.Count() > 1;

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
    Models.Settings.SetMapProviderTypeName(this.Context.Module, viewModel.MapProviderTypeName);

    // set module properties
    viewModel.MapSettings.SetSettings(this.Context.Site, this.Context.Module, ViewModels.Settings.DUMMY_APIKEY);

    // set map provider-specific properties
    PopulateProviderSettings(viewModel.MapProviderTypeName, settings, viewModel.ApiKey);

    await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

    //Render map here and save the image in the selected folder
    if (viewModel.SelectedFolder != null && !string.IsNullOrEmpty(viewModel.MapFileName))
    {
      viewModel.MapSettings.SetMapFileId (this.Context.Module, (await SaveRenderedMap(viewModel)).Id);

      await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);
    }

    return Ok();
  }

  private string GetApiKey(ViewModels.Settings viewModel, Models.Settings settings)
  {
    if (viewModel.ApiKey == ViewModels.Settings.DUMMY_APIKEY)
    {
      return settings.GetApiKey(this.Context.Site);
    }
    else
    {
      return viewModel.ApiKey;
    }
  }

  private void PopulateProviderSettings(string mapProviderName, Dictionary<string, string> values, string apiKey)
  {
    MapProvider mapProvider = MapsViewerController.GetMapProvider(this.MapProviders, mapProviderName);

    Models.Settings existingSettings = mapProvider.GetSettings(this.Context.Module);

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
      viewModel.MapProviderTypeName = String.IsNullOrEmpty(viewModel.MapProviderTypeName) ? Models.Settings.GetMapProviderTypeName(this.Context.Module) : viewModel.MapProviderTypeName;
      viewModel.MapProvider = MapsViewerController.GetMapProvider(this.MapProviders, viewModel.MapProviderTypeName);
      
      viewModel.MapSettings = viewModel.MapProvider?.GetSettings(this.Context.Module);
    }
    else
    {
      viewModel.MapProvider = MapsViewerController.GetMapProvider(this.MapProviders, viewModel.MapProviderTypeName);
    }

    viewModel.AvailableMapProviders = this.MapProviders
      .Select(provider => new ViewModels.Settings.AvailableMapProvider() 
        { 
          Name = GetFriendlyName(provider.GetType()), 
          TypeName=provider.GetType().FullName
        });

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

  private string GetFriendlyName(System.Type type)
  {
    System.ComponentModel.DisplayNameAttribute displayNameAttribute = type.GetCustomAttributes<System.ComponentModel.DisplayNameAttribute>(false)
      .FirstOrDefault();

    if (displayNameAttribute == null)
    {
      return $"{type.FullName}";
    }
    else
    {
      return displayNameAttribute.DisplayName;
    }
  }

  private async Task<Abstractions.Models.FileSystem.File> SaveRenderedMap(ViewModels.Settings viewModel)
  {
    MapProvider mapProvider = MapsViewerController.GetMapProvider(this.MapProviders, viewModel.MapProviderTypeName);

    Models.Settings settings = mapProvider.GetSettings(this.Context.Module);

    viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Id);
    
    using (System.IO.Stream mapFileStream = await mapProvider.Renderer.RenderMap(settings))
    {
      mapFileStream.Position = 0;
      return await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedFolder.Provider, viewModel.SelectedFolder.Path, viewModel.MapFileName, mapFileStream, true);
    }
  }

}