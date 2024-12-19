using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Maps.MapProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nucleus.Modules.Maps.Controllers
{
  [Extension("Maps")]
  public class MapsViewerController : Controller
  {
    private Context Context { get; }
    private IPageModuleManager PageModuleManager { get; }
    private IHttpClientFactory HttpClientFactory { get; }
    private IFileSystemManager FileSystemManager { get; }
    private IEnumerable<MapProvider> MapProviders { get; }

    public MapsViewerController(IHttpClientFactory httpClientFactory, Context Context, IEnumerable<MapProvider> mapProviders, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
    {
      this.Context = Context;
      this.MapProviders = mapProviders;
      this.PageModuleManager = pageModuleManager;
      this.HttpClientFactory = httpClientFactory;
      this.FileSystemManager = fileSystemManager;
    }

    [HttpGet]
    public ActionResult Index()
    {
      return View("Viewer", BuildViewModel());
    }

    [HttpGet]
    public async Task<ActionResult> RenderMap()
    {
      MapProvider mapProvider = GetMapProvider(this.MapProviders, Models.Settings.GetMapProviderTypeName(this.Context.Module));

      Models.Settings settings = mapProvider.GetSettings(this.Context.Module);

      if (settings.MapFileId != Guid.Empty)
      {
        File mapFile = await this.FileSystemManager.GetFile(this.Context.Site, settings.MapFileId);
        if (mapFile != null)
        {
          return File(await this.FileSystemManager.GetFileContents(this.Context.Site, mapFile), "image/png");
        }
      }

      return File(await mapProvider.Renderer.RenderMap(settings), "image/png");
    }

    internal static MapProvider GetMapProvider(IEnumerable<MapProvider> providers, string providerTypeName)
    {
      return providers.Where(provider => provider.GetType().FullName.Equals(providerTypeName)).FirstOrDefault();
    }

    private ViewModels.Viewer BuildViewModel()
    {
      ViewModels.Viewer viewModel = new();

      viewModel.MapProviderTypeName = Models.Settings.GetMapProviderTypeName(this.Context.Module);
      MapProvider mapProvider = GetMapProvider(this.MapProviders, Models.Settings.GetMapProviderTypeName(this.Context.Module));

      Models.Settings settings = mapProvider?.GetSettings(this.Context.Module);

      viewModel.Width = settings.Width;
      viewModel.Height = settings.Height;

      return viewModel;
    }
  }
}