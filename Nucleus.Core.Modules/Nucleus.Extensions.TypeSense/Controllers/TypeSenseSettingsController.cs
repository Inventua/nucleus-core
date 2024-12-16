using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.TypeSense.Controllers;

[Extension("TypeSense")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
public class TypeSenseSettingsController : Controller
{
  private Context Context { get; }
  private ISiteManager SiteManager { get; }
  private ISearchIndexHistoryManager SearchIndexHistoryManager { get; }
  private IConfiguration Configuration { get; }
  private ILogger<TypeSenseSettingsController> Logger { get; }
  private IHttpClientFactory HttpClientFactory { get; }

  public TypeSenseSettingsController(Context context, IConfiguration configuration, ISiteManager siteManager, IHttpClientFactory httpClientFactory, ISearchIndexHistoryManager searchIndexHistoryManager, ILogger<TypeSenseSettingsController> logger)
  {
    this.Context = context;
    this.Configuration = configuration;
    this.SiteManager = siteManager;
    this.SearchIndexHistoryManager = searchIndexHistoryManager;
    this.Logger = logger;
    this.HttpClientFactory = httpClientFactory;
  }

  [HttpGet]
  public ActionResult Settings()
  {
    return View("Settings", BuildSettingsViewModel(null));
  }

  [HttpPost]
  public ActionResult Settings(ViewModels.Settings viewModel)
  {
    return View("Settings", BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
  {
    viewModel.SaveSettings(this.Context.Site, viewModel.ApiKey);

    await this.SiteManager.Save(this.Context.Site);

    return Ok();
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> GetIndexCount(ViewModels.Settings viewModel)
  {
    TypeSenseRequest request = CreateRequest(this.Context.Site, viewModel);

    Typesense.CollectionResponse response = await request.GetIndexSettings();
    long indexCount = response.NumberOfDocuments;

    return Json(new { Title = "Index Count", Message = $"There are {indexCount} entries in the index.", Icon = "alert" });
  }


  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> ClearIndex(ViewModels.Settings viewModel)
  {
    TypeSenseRequest request = CreateRequest(this.Context.Site, viewModel);


    if (await request.ClearIndex())
    {
      await this.SearchIndexHistoryManager.Delete(this.Context.Site.Id);

      return Json(new { Title = "Clear Index", Message = $"Index '{viewModel.IndexName}' has been deleted and re-created.", Icon = "alert" });
    }
    else
    {
      return Json(new { Title = "Clear Index", Message = $"Index '{viewModel.IndexName}' was not removed.", Icon = "error" });
    }
  }

  //[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  //[HttpPost]
  //public async Task<ActionResult> GetIndexSettings(ViewModels.Settings viewModel)
  //{
  //  TypeSenseRequest request = CreateRequest(this.Context.Site, viewModel);

  //  Typesense.CollectionResponse response = await request.GetIndexSettings();
    
  //  if (response != null)
  //  {
  //    return Json(new { Title = "Get Index Settings", Message = System.Net.WebUtility.HtmlEncode(response.ToString()) });
  //  }
  //  else
  //  {
  //    return Json(new { Title = "Get Index Settings", Message = System.Net.WebUtility.HtmlEncode(response.ToString()) });
  //  }
  //}

  private string GetApiKey(ViewModels.Settings viewModel)
  {
    if (viewModel.ApiKey == ViewModels.Settings.DUMMY_APIKEY)
    {
      Models.Settings settings = new(this.Context.Site);
      return Models.Settings.DecryptApiKey(this.Context.Site, settings.EncryptedApiKey);
    }
    else
    {
      return viewModel.ApiKey;
    }
  }


  private TypeSenseRequest CreateRequest(Site site, ViewModels.Settings settings)
  {
    return new
    (
      this.HttpClientFactory,
      new System.Uri(settings.ServerUrl),
      settings.IndexName,
      GetApiKey(settings),
      this.Logger
    );
  }

  private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new(this.Context.Site);
    }

    return viewModel;
  }
}