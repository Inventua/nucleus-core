using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.MapProviders;
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

    public MapsViewerController(IHttpClientFactory httpClientFactory, Context Context, IPageModuleManager pageModuleManager)
    {
      this.Context = Context;
      this.PageModuleManager = pageModuleManager;
      this.HttpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public ActionResult Index()
    {
      return View("Viewer", BuildViewModel());
    }

    [HttpGet]
    public async Task<ActionResult> RenderMap(string provider)
    {
      IMapProvider mapProvider = GetMapProvider(provider);

      Models.Settings settings = mapProvider.GetSettings();

      settings.GetSettings(this.Context.Module);

      return File (await mapProvider.GetRenderer().RenderMap(this.Context.Site, this.HttpClientFactory, settings), "image/png");
    }

    internal static IMapProvider GetMapProvider(string  provider)
    {
      return provider == "AzureMaps" ? new AzureMapProvider() : new GoogleMapProvider();
    }

    private ViewModels.Viewer BuildViewModel()
    {
      ViewModels.Viewer viewModel = new();
            
      viewModel.MapProvider = Models.Settings.GetMapProvider(this.Context.Module);

      return viewModel;
    }
  }
}