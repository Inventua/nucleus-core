using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AzureSearch.Controllers;

[Extension("AzureSearch")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class AzureSearchSettingsController : Controller
{
  private Context Context { get; }
  private ISiteManager SiteManager { get; }
  private ISearchIndexHistoryManager SearchIndexHistoryManager { get; }

  public AzureSearchSettingsController(Context context, ISiteManager siteManager, ISearchIndexHistoryManager searchIndexHistoryManager)
  {
    this.Context = context;
    this.SiteManager = siteManager;

    this.SearchIndexHistoryManager = searchIndexHistoryManager;
  }

  [HttpGet]
  [HttpPost]
  public ActionResult Settings()
  {
    return View("Settings",  BuildSettingsViewModel());
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
  {
    if (!viewModel.ServerUrl.StartsWith("http"))
    {
      viewModel.ServerUrl = "http://" + viewModel.ServerUrl;
    }

    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_SERVER_URL, viewModel.ServerUrl);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_INDEX_NAME, viewModel.IndexName);

    if (viewModel.ApiKey != ViewModels.Settings.DUMMY_APIKEY)
    {
      this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_SERVER_APIKEY, ConfigSettings.EncryptApiKey(this.Context.Site, viewModel.ApiKey));
    }

    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_ATTACHMENT_MAXSIZE, viewModel.AttachmentMaxSize);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_INDEXING_PAUSE, viewModel.IndexingPause);

    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_TITLE, viewModel.Boost.Title);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_SUMMARY, viewModel.Boost.Summary);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_CATEGORIES, viewModel.Boost.Categories);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_KEYWORDS, viewModel.Boost.Keywords);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_CONTENT, viewModel.Boost.Content);

    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_ATTACHMENT_AUTHOR, viewModel.Boost.AttachmentAuthor);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_ATTACHMENT_KEYWORDS, viewModel.Boost.AttachmentKeywords);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_ATTACHMENT_NAME, viewModel.Boost.AttachmentName);
    this.Context.Site.SiteSettings.TrySetValue(ConfigSettings.SITESETTING_BOOST_ATTACHMENT_TITLE, viewModel.Boost.AttachmentTitle);

    await this.SiteManager.Save(this.Context.Site);

    return Ok();
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> GetIndexCount(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = new(new System.Uri(viewModel.ServerUrl), GetApiKey(viewModel), viewModel.IndexName);

    Azure.Search.Documents.Indexes.Models.SearchIndexStatistics response = await request.GetIndexSettings();
    long indexCount = response.DocumentCount;

    return Json(new { Title = "Index Count", Message = $"There are {indexCount} entries in the index.", Icon = "alert" });
  }


  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> ClearIndex(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = new(new System.Uri(viewModel.ServerUrl), GetApiKey(viewModel), viewModel.IndexName);
    
    if (await request.DeleteIndex())
    {
      await this.SearchIndexHistoryManager.Delete(this.Context.Site.Id);
      return Json(new { Title = "Clear Index", Message = $"Index '{viewModel.IndexName}' has been removed and will be re-created the next time the search index feeder runs.", Icon = "alert" });
    }
    else
    {
      return Json(new { Title = "Clear Index", Message = $"Index '{viewModel.IndexName}' was not removed.", Icon = "error" });
    }
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> GetIndexSettings(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = new(new System.Uri(viewModel.ServerUrl), GetApiKey(viewModel), viewModel.IndexName);

    Azure.Search.Documents.Indexes.Models.SearchIndexStatistics response = await request.GetIndexSettings();

    if (response != null)
    {
      return Json(new { Title = "Get Index Settings", Message = System.Net.WebUtility.HtmlEncode(response.ToString()) });
    }
    else
    {
      return Json(new { Title = "Get Index Settings", Message = System.Net.WebUtility.HtmlEncode(response.ToString()) });
    }
  }

  private string GetApiKey(ViewModels.Settings viewModel)
  {
    if (viewModel.ApiKey == ViewModels.Settings.DUMMY_APIKEY)
    {
      ConfigSettings settings = new(this.Context.Site);
      return ConfigSettings.DecryptApiKey(this.Context.Site, settings.EncryptedApiKey);
    }
    else
    {
      return viewModel.ApiKey;
    }
  }


  private ViewModels.Settings BuildSettingsViewModel()
  {
    return new(this.Context.Site);
  }
}