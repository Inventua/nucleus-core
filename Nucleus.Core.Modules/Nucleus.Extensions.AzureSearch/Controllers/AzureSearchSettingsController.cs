using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nucleus.Extensions.AzureSearch.Controllers;

[Extension("AzureSearch")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class AzureSearchSettingsController : Controller
{
  private Context Context { get; }
  private ISiteManager SiteManager { get; }
  private ISearchIndexHistoryManager SearchIndexHistoryManager { get; }
  private IFileSystemManager FileSystemManager { get; } 
  private IConfiguration Configuration { get; }

  public AzureSearchSettingsController(Context context, IConfiguration configuration, ISiteManager siteManager, IFileSystemManager fileSystemManager, ISearchIndexHistoryManager searchIndexHistoryManager)
  {
    this.Context = context;
    this.Configuration = configuration;
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
    this.SearchIndexHistoryManager = searchIndexHistoryManager;
  }

  [HttpGet]
  public async Task<ActionResult> Settings()
  {
    return View("Settings", await BuildSettingsViewModel(null));
  }

  [HttpPost]
  public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
  {
    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
  {
    viewModel.SaveSettings(this.Context.Site, viewModel.ApiKey, viewModel.AzureOpenAIApiKey);

    await this.SiteManager.Save(this.Context.Site);

    return Ok();
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> GetIndexCount(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);

    Azure.Search.Documents.Indexes.Models.SearchIndexStatistics response = await request.GetIndexSettings();
    long indexCount = response.DocumentCount;

    return Json(new { Title = "Index Count", Message = $"There are {indexCount} entries in the index.", Icon = "alert" });
  }


  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> ClearIndex(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);


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

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> CreateIndexer(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);
    IReadOnlyList<FileSystemProviderInfo> providers = this.FileSystemManager.ListProviders();

    IEnumerable<FileSystemProviderInfo> azureProviders = providers.Where(provider => provider.ProviderType.Contains("AzureBlobStorageFileSystemProvider"));

    if (!azureProviders.Any())
    {
      return BadRequest("Cannot automatically create an indexer because there are no Azure Blob Storage file system providers configured for Nucleus.");
    }
    else if (azureProviders.Count() > 1) 
    {
      return BadRequest("Cannot automatically create an indexer because there are multiple Azure Blob Storage file system providers configured for Nucleus.");
    }

    // ensure that index exists
    if (await request.CanConnect(this.Context.Site))
    {
      // create an indexer for the configured Azure Blob Storage provider
      for (int count = 1; count < providers.Count + 1; count++)
      {
        string configKeyPrefix = $"{Nucleus.Abstractions.Models.Configuration.FileSystemProviderFactoryOptions.Section}:Providers:{count}";
        if (this.Configuration.GetValue<string>($"{configKeyPrefix}:Key") == azureProviders.First().Key)
        {
          string connectionString = this.Configuration.GetValue<string>($"{configKeyPrefix}:ConnectionString");
          string rootPath = this.Configuration.GetValue<string>($"{configKeyPrefix}:RootPath");

          viewModel.IndexerName = await request.CreateIndexer(azureProviders.First().Key, connectionString, rootPath, this.Context.Site.HomeDirectory);
          break;
        }
      }
    }

    ModelState.Clear();

    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> AddSemanticRanking(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);

    // ensure that index exists
    if (await request.CanConnect(this.Context.Site))
    {
      try
      { 
        viewModel.SemanticConfigurationName = await request.AddSemanticRanking();
        
        viewModel.SaveSettings(this.Context.Site, viewModel.ApiKey, viewModel.AzureOpenAIApiKey);
        await this.SiteManager.Save(this.Context.Site);
      }
      catch (Azure.RequestFailedException ex)
      {
        return Json(new { Title = "Add Semantic Ranking", Message = ex.Message, Icon = "alert" });
      }
    }

    ModelState.Clear();

    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> AddVectorization(ViewModels.Settings viewModel)
  {
    if (string.IsNullOrEmpty(viewModel.AzureOpenAIEndpoint))
    {
      ModelState.AddModelError<ViewModels.Settings>(viewModel => viewModel.AzureOpenAIEndpoint, "Azure Open AI endpoint is required.");
    }

    if (string.IsNullOrEmpty(viewModel.AzureOpenAIApiKey))
    {
      ModelState.AddModelError<ViewModels.Settings>(viewModel => viewModel.AzureOpenAIApiKey, "Azure Open AI key is required.");
    }

    if (string.IsNullOrEmpty(viewModel.AzureOpenAIDeploymentName))
    {
      ModelState.AddModelError<ViewModels.Settings>(viewModel => viewModel.AzureOpenAIDeploymentName, "Azure Open AI deployment name is required.");
    }

    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);

    // ensure that index exists
    if (await request.CanConnect(this.Context.Site))
    {
      try
      {
        viewModel.VectorizationEnabled = await request.AddVectorization();
        viewModel.SaveSettings(this.Context.Site, viewModel.ApiKey, viewModel.AzureOpenAIApiKey);
        await this.SiteManager.Save(this.Context.Site);
      }
      catch (Azure.RequestFailedException ex)
      {
        return Json(new { Title = "Add Vectorization", Message = ex.Message, Icon = "alert" });       
      }
    }

    ModelState.Clear();

    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> GetIndexSettings(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);

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

  private string GetOpenAIApiKey(ViewModels.Settings viewModel)
  {
    if (viewModel.AzureOpenAIApiKey == ViewModels.Settings.DUMMY_APIKEY)
    {
      ConfigSettings settings = new(this.Context.Site);
      return ConfigSettings.DecryptApiKey(this.Context.Site, settings.EncryptedAzureOpenAIApiKey);
    }
    else
    {
      return viewModel.AzureOpenAIApiKey;
    }
  }

  private AzureSearchRequest CreateRequest(Site site, ViewModels.Settings settings)
  {
    return new
    (
      new System.Uri(settings.ServerUrl),
      GetApiKey(settings),
      settings.IndexName,
      settings.IndexerName,
      settings.SemanticConfigurationName,
      settings.VectorizationEnabled,
      settings.AzureOpenAIEndpoint,
      GetOpenAIApiKey(settings),
      settings.AzureOpenAIDeploymentName
    );
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new(this.Context.Site);
    }

    viewModel.Indexers = [];
    viewModel.Semanticonfigurations = [];

    if (!string.IsNullOrEmpty(viewModel.ServerUrl) || !string.IsNullOrEmpty(GetApiKey(viewModel)) || !string.IsNullOrEmpty(viewModel.IndexName))
    {
      var request = CreateRequest(this.Context.Site, viewModel);

      try
      {
        viewModel.VectorizationEnabled = await request.IsVectorizationConfigured();
        viewModel.Indexers = await request.ListIndexers();
        viewModel.Semanticonfigurations = await request.ListSemanticRankingConfigurations();
      }
      catch (Azure.RequestFailedException ex)
      {
        if (ex.Status == 404)
        {
          // suppress in case the index has been removed
          viewModel.AzureOpenAIEndpoint= "";
          viewModel.AzureOpenAIApiKey = "";
          viewModel.AzureOpenAIDeploymentName = "";

          viewModel.SemanticConfigurationName = "";
          viewModel.IndexerName = "";
          viewModel.SemanticConfigurationName = "";

          viewModel.VectorizationEnabled = false;
        }
        else
        {
          throw;
        }
      }
    }

    return viewModel;
  }
}