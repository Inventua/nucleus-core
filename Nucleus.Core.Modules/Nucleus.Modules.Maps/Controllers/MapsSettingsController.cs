using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.MapProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.Maps.Controllers;

[Extension("Maps")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class MapsSettingsController : Controller
{
  private IWebHostEnvironment WebHostEnvironment { get; }
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }

  private const string VIEWER_DIRECTORY = "Extensions/Maps/Views/MapViews/";

  public MapsSettingsController(IWebHostEnvironment webHostEnvironment, Context Context, IPageModuleManager pageModuleManager)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
  }

  [HttpGet]
  public ActionResult Settings()
  {
    return View("Settings", BuildSettingsViewModel(null));
  }

  [HttpPost]
  public ActionResult Settings(ViewModels.Settings viewModel)
  {
    ModelState.Clear();

    return View("Settings", BuildSettingsViewModel(viewModel));
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

    // set shared properties
    viewModel.MapSettings.SetSettings(this.Context.Site, this.Context.Module, ViewModels.Settings.DUMMY_APIKEY);

    // set map provider-specific properties
    PopulateSettings(viewModel.MapProvider, settings, viewModel.ApiKey);

    await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

    return Ok();
  }

  private void PopulateSettings(string mapProviderName, Dictionary<string, string> values, string apiKey)
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

  private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    viewModel ??= new();

    viewModel.MapProvider = String.IsNullOrEmpty(viewModel.MapProvider) ? Models.Settings.GetMapProvider(this.Context.Module) : viewModel.MapProvider;

    viewModel.MapSettings = MapsViewerController.GetMapProvider(viewModel.MapProvider).GetSettings();
    viewModel.MapSettings.GetSettings(this.Context.Module);

    viewModel.MapProviders = new();
    foreach (IFileInfo file in this.WebHostEnvironment.ContentRootFileProvider.GetDirectoryContents(VIEWER_DIRECTORY)
      .Where(fileInfo => System.IO.Path.GetExtension(fileInfo.Name).Equals(".cshtml", StringComparison.OrdinalIgnoreCase))
      .OrderBy(fileInfo => fileInfo.Name))
    {
      viewModel.MapProviders.Add(System.IO.Path.GetFileNameWithoutExtension(file.Name));
    }

    viewModel.MapProviderSettingsView = $"MapSettings/_{viewModel.MapProvider}Settings.cshtml";

    return viewModel;
  }

}