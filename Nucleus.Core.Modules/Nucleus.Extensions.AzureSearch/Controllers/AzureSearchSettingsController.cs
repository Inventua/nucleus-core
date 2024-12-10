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
using Microsoft.Extensions.Logging;
using Azure.Search.Documents.Indexes.Models;

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
  private ILogger<AzureSearchSettingsController> Logger { get; }

  public AzureSearchSettingsController(Context context, IConfiguration configuration, ISiteManager siteManager, IFileSystemManager fileSystemManager, ISearchIndexHistoryManager searchIndexHistoryManager, ILogger<AzureSearchSettingsController> logger)
  {
    this.Context = context;
    this.Configuration = configuration;
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
    this.SearchIndexHistoryManager = searchIndexHistoryManager;
    this.Logger = logger;
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
    viewModel.SaveSettings(this.Context.Site, viewModel.AzureSearchApiKey, viewModel.AzureOpenAIApiKey);

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
  public async Task<ActionResult> CreateIndexer(ViewModels.Settings viewModel, string key)
  {
    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);
    IReadOnlyList<FileSystemProviderInfo> providers = this.FileSystemManager.ListProviders();

    FileSystemProviderInfo azureProvider = providers.Where(provider => provider.ProviderType.Contains("AzureBlobStorageFileSystemProvider") && provider.Key == key).FirstOrDefault();

    // ensure that index exists
    if (await request.CanConnect(this.Context.Site))
    {
      // create an indexer for the configured Azure Blob Storage provider
      for (int count = 1; count < providers.Count + 1; count++)
      {
        string configKeyPrefix = $"{Nucleus.Abstractions.Models.Configuration.FileSystemProviderFactoryOptions.Section}:Providers:{count}";
        if (this.Configuration.GetValue<string>($"{configKeyPrefix}:Key") == azureProvider.Key)
        {
          string connectionString = this.Configuration.GetValue<string>($"{configKeyPrefix}:ConnectionString");
          string rootPath = this.Configuration.GetValue<string>($"{configKeyPrefix}:RootPath");

          await request.CreateIndexer(azureProvider.Key, connectionString, rootPath, this.Context.Site.HomeDirectory);
          break;
        }
      }
    }

    ModelState.Clear();

    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> RemoveIndexer(ViewModels.Settings viewModel, string indexerName)
  {
    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);
    await request.DeleteIndexer(indexerName);

    ModelState.Clear();

    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> CreateIndex(ViewModels.Settings viewModel)
  {
    AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);

    try
    {
      viewModel.IndexName = await request.CreateIndex(viewModel.NewIndexName);

      viewModel.SaveSettings(this.Context.Site, viewModel.AzureSearchApiKey, viewModel.AzureOpenAIApiKey);
      await this.SiteManager.Save(this.Context.Site);
    }
    catch (Azure.RequestFailedException ex)
    {
      return Json(new { Title = "Create Index", Message = ex.Message, Icon = "alert" });
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
        viewModel.SemanticConfigurationName = await request.AddSemanticRanking(viewModel.NewSemanticRankingConfigurationName);

        viewModel.SaveSettings(this.Context.Site, viewModel.AzureSearchApiKey, viewModel.AzureOpenAIApiKey);
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
    if (string.IsNullOrEmpty(viewModel.OpenAIServiceSettings.AzureOpenAIEndpoint))
    {
      ModelState.AddModelError<ViewModels.Settings>(viewModel => viewModel.OpenAIServiceSettings.AzureOpenAIEndpoint, "Azure Open AI endpoint is required.");
    }

    if (string.IsNullOrEmpty(viewModel.AzureOpenAIApiKey))
    {
      ModelState.AddModelError<ViewModels.Settings>(viewModel => viewModel.AzureOpenAIApiKey, "Azure Open AI key is required.");
    }

    if (string.IsNullOrEmpty(viewModel.OpenAIServiceSettings.AzureOpenAIEmbeddingModelDeploymentName))
    {
      ModelState.AddModelError<ViewModels.Settings>(viewModel => viewModel.OpenAIServiceSettings.AzureOpenAIEmbeddingModelDeploymentName, "Azure Open AI deployment name is required.");
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
        viewModel.UseVectorSearch = await request.AddVectorization();
        viewModel.SaveSettings(this.Context.Site, viewModel.AzureSearchApiKey, viewModel.AzureOpenAIApiKey);
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

  private string GetAzureSearchApiKey(ViewModels.Settings viewModel)
  {
    if (viewModel.AzureSearchApiKey == ViewModels.Settings.DUMMY_APIKEY)
    {
      AzureSearchSettings settings = new(this.Context.Site);
      return AzureSearchSettings.Decrypt(this.Context.Site, settings.AzureSearchServiceEncryptedApiKey);
    }
    else
    {
      return viewModel.AzureSearchApiKey;
    }
  }

  private string GetOpenAIApiKey(ViewModels.Settings viewModel)
  {
    if (viewModel.AzureOpenAIApiKey == ViewModels.Settings.DUMMY_APIKEY)
    {
      AzureSearchSettings settings = new(this.Context.Site);
      return AzureSearchSettings.Decrypt(this.Context.Site, settings.OpenAIServiceSettings.AzureOpenAIEncryptedApiKey);
    }
    else
    {
      return viewModel.AzureOpenAIApiKey;
    }
  }

  private AzureSearchRequest CreateRequest(Site site, ViewModels.Settings settings)
  {
    settings.AzureSearchServiceEncryptedApiKey = AzureSearchSettings.Encrypt(site, GetAzureSearchApiKey(settings));
    settings.OpenAIServiceSettings.AzureOpenAIEncryptedApiKey = AzureSearchSettings.Encrypt(site, GetOpenAIApiKey(settings));

    return new
    (
      site,
      settings,
      this.Logger
    );
    ////return new
    ////(
    ////  new System.Uri(settings.AzureSearchServiceEndpoint),
    ////  GetApiKey(settings),
    ////  settings.IndexName,
    ////  settings.IndexerName,
    ////  settings.SemanticConfigurationName,
    ////  settings.VectorizationEnabled,
    ////  settings.AzureOpenAIEndpoint,
    ////  GetOpenAIApiKey(settings),
    ////  settings.AzureOpenAIDeploymentName,
    ////  this.Logger
    ////);
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new(this.Context.Site);
    }

    viewModel.SearchIndexes = [];
    viewModel.DataSources = [];
    viewModel.Semanticonfigurations = [];

    if (!string.IsNullOrEmpty(viewModel.AzureSearchServiceEndpoint) || !string.IsNullOrEmpty(GetAzureSearchApiKey(viewModel)) || !string.IsNullOrEmpty(viewModel.IndexName))
    {
      AzureSearchRequest request = CreateRequest(this.Context.Site, viewModel);
      
      SearchIndex searchIndex = await request.GetIndex();

      try
      {
        viewModel.UseVectorSearch = await request.IsVectorizationConfigured();
        viewModel.SearchIndexes = (await request.ListIndexes(this.Context.Site))
          .Select(index => index.Name)
          .ToList();

        List<SearchIndexer> indexers = await request.ListIndexers();
        viewModel.Semanticonfigurations = await request.ListSemanticRankingConfigurations();

        if (string.IsNullOrEmpty(viewModel.SemanticConfigurationName))
        {
          viewModel.SemanticConfigurationName= searchIndex?.SemanticSearch?.DefaultConfigurationName;
        }

        IReadOnlyList<FileSystemProviderInfo> providers = this.FileSystemManager.ListProviders();
        IEnumerable<FileSystemProviderInfo> azureProviders = providers.Where(provider => provider.ProviderType.Contains("AzureBlobStorageFileSystemProvider"));

        foreach (var provider in azureProviders)
        {
          viewModel.DataSources.Add(new
            (
              provider, 
                indexers
                  .Where(indexer=>indexer.TargetIndexName.Equals(viewModel.IndexName))
                .Select(indexer => indexer.Name)
                .FirstOrDefault()
            )
          );
        }
      }
      catch (Azure.RequestFailedException ex)
      {
        if (ex.Status == 404)
        {
          // suppress in case the index has been removed
          viewModel.OpenAIServiceSettings ??= new();
          viewModel.SemanticConfigurationName = "";

          viewModel.UseVectorSearch = false;
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