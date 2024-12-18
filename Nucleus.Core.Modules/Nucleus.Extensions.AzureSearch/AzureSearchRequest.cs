using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using OpenAI.Embeddings;

// https://learn.microsoft.com/en-us/azure/search/index-projections-concept-intro?tabs=kstore-rest

namespace Nucleus.Extensions.AzureSearch;

internal partial class AzureSearchRequest
{
  // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/search.documents-readme?view=azure-dotnet


  public Site Site { get; }

  public AzureSearchSettings Settings { get; }
  private ILogger Logger { get; }

  private SearchIndexClient _searchIndexClient { get; set; }
  private SearchClient _searchClient { get; set; }

  internal const string DOCUMENT_ENTITY_NAME = "document";
  internal const string CHUNK_ENTITY_NAME = "chunk";
  internal const string VECTOR_ENTITY_NAME = "content_vector";

  internal static readonly char[] WORD_BREAKING_CHARS = [' ', ',', ';', ':', '<', '>', '.', '!', '#', '_', '/', '\\', '\t', '\n', '\r'];

  internal const int WORDS_PER_CHUNK = IndexExtensions.SPLITTER_CHUNK_SIZE;

  internal const string ID_CHUNK_REGEX = ".*_chunks_(?<chunk>[0-9]*)";

  [System.Text.RegularExpressions.GeneratedRegex(ID_CHUNK_REGEX)]
  private static partial System.Text.RegularExpressions.Regex GET_CHUNK_PAGE_ID_REGEX();

  // this list is from https://learn.microsoft.com/en-us/azure/search/cognitive-search-skill-document-extraction#supported-document-formats
  // we set the indexer "included extensions" so that Azure Search doesn't try to run the skill set for documents which won't have content, to avoid warnings
  private static readonly string[] INDEXER_EXTENSIONS = [".txt", ".csv", ".eml", ".epub", ".gz", ".html", ".json", ".kml", ".docx", ".doc", ".docm", ".xlsx", ".xls", ".xlsm", ".pptx", ".ppt", ".pptm", ".msg", ".xml", ".odt", ".ods", ".odp", ".pdf", ".rtf", ".xml", ".zip"];

  public AzureSearchRequest(Site site, AzureSearchSettings settings, ILogger logger)
  {
    this.Site = site;
    this.Settings = settings;
    this.Logger = logger;
  }

  /// <summary>
  /// Return a client which can be used to manage indexes
  /// </summary>
  /// <returns></returns>
  private async Task<SearchIndexClient> SearchIndexClient()
  {
    if (this._searchIndexClient == null)
    {
      SearchIndexClient client = CreateIndexClient();
      this._searchIndexClient = client;
    }

    return _searchIndexClient;
  }

  /// <summary>
  /// Return a client which can be used to retrieve results from (search) indexes
  /// </summary>
  /// <returns></returns>
  private async Task<SearchClient> GetSearchClient()
  {
    if (_searchClient == null)
    {
      _searchClient = (await this.SearchIndexClient()).GetSearchClient(this.Settings.IndexName);
    }

    return _searchClient;
  }

  private SearchIndexClient CreateIndexClient()
  {
    JsonSerializerOptions serializerOptions = new()
    {
      DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    Azure.Core.Serialization.JsonObjectSerializer serializer = new(serializerOptions);

    SearchClientOptions options = new()
    {
      Serializer = serializer
    };

    return new SearchIndexClient(new(this.Settings.AzureSearchServiceEndpoint), new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey)), options);
  }

  public Boolean Equals(string url, string indexName, string encryptedApiKey)
  {
    return this.Settings.AzureSearchServiceEndpoint == url && this.Settings.IndexName == indexName && this.Settings.AzureSearchServiceEncryptedApiKey == encryptedApiKey;
  }

  public async Task<Boolean> CanConnect(Site site)
  {
    if (site == null)
    {
      throw new NullReferenceException("site must not be null.");
    }

    AzureSearchSettings settings = new(site);
    try
    {
      Response<SearchIndexStatistics> result = await (await this.SearchIndexClient()).GetIndexStatisticsAsync(settings.IndexName);
      return true;
    }
    catch (Exception)
    {
      return false;
    }
  }



  public async Task<string> CreateIndex(string name)
  {
    SearchIndexClient client = CreateIndexClient();

    List<string> vectorFields = [nameof(AzureSearchDocument.TitleVector), nameof(AzureSearchDocument.SummaryVector), nameof(AzureSearchDocument.ContentVector)];
    SearchIndex searchIndex;

    try
    {
      // index exists, update it
      searchIndex = await client.GetIndexAsync(name);
    }
    catch (Azure.RequestFailedException)
    {
      // index does not exist
      searchIndex = null;
    }

    if (searchIndex == null)
    {
      FieldBuilder builder = new();
      IList<SearchField> fields = builder.Build(typeof(AzureSearchDocument))
        // exclude vector fields from the default index. They are added later if vectorization 
        // is enabled
        .Where(field => !vectorFields.Contains(field.Name))
        .ToList();

      searchIndex = new(name, fields);
    }

    searchIndex.Similarity = new BM25Similarity() { B = 0.75, K1 = 1.2 };

    searchIndex
     .AddSuggesters(this.Settings)
     .AddScoringProfiles(this.Settings, false);

    Response<SearchIndex> createIndexResponse = await client.CreateOrUpdateIndexAsync(searchIndex, true);

    return name;
  }

  public async Task<List<SearchIndexer>> ListIndexers()
  {
    SearchIndexerClient client = new(new(this.Settings.AzureSearchServiceEndpoint), new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey)));
    var response = await client.GetIndexersAsync();

    return response.Value.ToList();
  }

  public async Task<List<string>> ListSemanticRankingConfigurations()
  {
    SearchIndexClient client = await this.SearchIndexClient();
    Response<SearchIndex> response = await client.GetIndexAsync(this.Settings.IndexName);

    return response.Value.SemanticSearch?.Configurations.Select(config => config.Name).ToList() ?? [];
  }

  public async Task<string> CreateIndexer(string key, string connectionString, string rootPath, string folder)
  {
    // https://learn.microsoft.com/en-us/azure/search/index-projections-concept-intro?tabs=kstore-rest

    if (string.IsNullOrEmpty(rootPath))
    {
      rootPath = folder;
      folder = "";
    }
    PathUri path = new(rootPath, folder);
    SearchIndexerClient client = new(new(this.Settings.AzureSearchServiceEndpoint), new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey)));

    // create or update a data source
    SearchIndexerDataSourceConnection dataSource = new
    (
      $"data-source-{key}".ToLower(),
      SearchIndexerDataSourceType.AzureBlob,
      connectionString,
      new SearchIndexerDataContainer(path.ContainerName) { Query = path.RelativePath }
    )
    {
      Description = $"Auto-generated indexer data source for the {this.Settings.IndexName} index. This data source was created by Nucleus."
    };

    Response<SearchIndexerDataSourceConnection> createDataSourceResponse = await client.CreateOrUpdateDataSourceConnectionAsync(dataSource);

    // create a BLOB storage indexer
    SearchIndexer searchIndexer = new($"indexer-{key}-{this.Settings.IndexName}".ToLower(), dataSource.Name, this.Settings.IndexName)
    {
      Description = $"Auto-generated storage indexer for the {this.Settings.IndexName} index. This indexer was created by Nucleus.",

      Parameters = new()
      {
        IndexingParametersConfiguration = new()
        {
          {"indexedFileNameExtensions", string.Join(',', INDEXER_EXTENSIONS)},
          {"excludedFileNameExtensions", ""},
          {"failOnUnsupportedContentType", false},
          {"indexStorageMetadataOnlyForOversizedDocuments", true},
          {"dataToExtract", "contentAndMetadata"},
          {"parsingMode", "default"},
        }
      }
    };

    // create skill set, if vectorization is enabled
    if (this.Settings.UseVectorSearch)
    {
      searchIndexer.SkillsetName = (await CreateSkillSet()).Name;
    }

    var createIndexerResponse = await client.CreateOrUpdateIndexerAsync(searchIndexer);

    return searchIndexer.Name;
  }

  public async Task DeleteIndexer(string name)
  {
    // https://learn.microsoft.com/en-us/azure/search/index-projections-concept-intro?tabs=kstore-rest

    SearchIndexerClient client = new(new(this.Settings.AzureSearchServiceEndpoint), new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey)));

    Response<SearchIndexer> indexer = await client.GetIndexerAsync(name);

    if (indexer != null)
    {
      // remove indexer
      await client.DeleteIndexerAsync(indexer.Value.Name);

      // remove data source
      await client.DeleteDataSourceConnectionAsync(indexer.Value.DataSourceName);

      // look for a skill set & remove it if no other indexers reference it
      if (this.Settings.UseVectorSearch)
      {
        string skillSetName = this.BuildSkillsetName();
        bool skillSetInUse = false;
        foreach (var otherIndexer in (await client.GetIndexersAsync()).Value)
        {
          if (otherIndexer.SkillsetName.Equals(skillSetName))
          {
            skillSetInUse = true;
          }
        }

        if (!skillSetInUse)
        {
          await client.DeleteSkillsetAsync(skillSetName);
        }
      }
    }
  }

  public async Task<SearchIndexerSkillset> CreateSkillSet()
  {
    // create skill set

    // this code creates a text-splitting skill and embedding (vector generation) skill for the file content. Generating vectors for the 
    // title and summary fields is handled by the push feed.
    SplitSkill splitSkill = new
    (
      inputs: [new InputFieldMappingEntry("text") { Source = $"/{DOCUMENT_ENTITY_NAME}/content" }],
      outputs: [new OutputFieldMappingEntry("textItems") { TargetName = CHUNK_ENTITY_NAME }]
    )
    {
      Name = "Text Splitter",
      DefaultLanguageCode = "en",
      TextSplitMode = TextSplitMode.Pages,
      MaximumPageLength = IndexExtensions.SPLITTER_CHUNK_SIZE,
      //MaximumPagesToTake = 0,
      Unit = SplitSkillUnit.AzureOpenAITokens
    };

    AzureOpenAIEmbeddingSkill contentVectorizationSkill = new
    (
      inputs: [new InputFieldMappingEntry("text") { Source = $"/{DOCUMENT_ENTITY_NAME}/{CHUNK_ENTITY_NAME}/*" }],
      outputs: [new OutputFieldMappingEntry("embedding") { TargetName = $"{VECTOR_ENTITY_NAME}" }]
    )
    {
      Name = "Vector Embedding - Content",
      ResourceUri = new Uri(this.Settings.OpenAIServiceSettings.AzureOpenAIEndpoint),
      ApiKey = AzureSearchSettings.Decrypt(this.Site, this.Settings.OpenAIServiceSettings.AzureOpenAIEncryptedApiKey),
      DeploymentName = this.Settings.OpenAIServiceSettings.AzureOpenAIEmbeddingModelDeploymentName,
      Context = $"/{DOCUMENT_ENTITY_NAME}/{CHUNK_ENTITY_NAME}/*",
      Dimensions = IndexExtensions.VECTOR_DIMENSIONS,
      ModelName = IndexExtensions.VECTORIZER_MODEL_NAME
    };

    SearchIndexerSkillset indexerSkillset = new
    (
      name: this.BuildSkillsetName(),
      skills: [splitSkill, contentVectorizationSkill]
    );

    SearchIndexerIndexProjectionSelector projectionSelector = new
    (
      targetIndexName: this.Settings.IndexName,
      parentKeyFieldName: nameof(AzureSearchDocument.ParentId),
      sourceContext: $"/{DOCUMENT_ENTITY_NAME}/{CHUNK_ENTITY_NAME}/*",
      mappings:
      [
        new InputFieldMappingEntry(nameof(AzureSearchDocument.Url)) { Source = $"/{DOCUMENT_ENTITY_NAME}/metadata_storage_path" },
        new InputFieldMappingEntry(nameof(AzureSearchDocument.Title)) { Source = $"/{DOCUMENT_ENTITY_NAME}/metadata_storage_name" },
        new InputFieldMappingEntry(nameof(AzureSearchDocument.Content)) { Source = $"/{DOCUMENT_ENTITY_NAME}/{CHUNK_ENTITY_NAME}/*" },
        new InputFieldMappingEntry(nameof(AzureSearchDocument.ContentVector)) { Source = $"/{DOCUMENT_ENTITY_NAME}/{CHUNK_ENTITY_NAME}/*/{VECTOR_ENTITY_NAME}" }
      ]);

    indexerSkillset.IndexProjection = new SearchIndexerIndexProjection
    (
      selectors: [projectionSelector]
    );

    SearchIndexerClient client = new
    (
      endpoint: new(this.Settings.AzureSearchServiceEndpoint),
      credential: new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey))
    );
    Response<SearchIndexerSkillset> skillsetResponse = await client.CreateOrUpdateSkillsetAsync(indexerSkillset);

    return skillsetResponse.Value;
  }

  private string BuildSkillsetName()
  {
    return $"skillset-vectorgeneration-{this.Settings.IndexName}";
  }

  public async Task<SearchIndex> GetIndex()
  {
    try
    {
      SearchIndexClient client = await this.SearchIndexClient();
      return await client.GetIndexAsync(this.Settings.IndexName);
    }
    catch (Azure.RequestFailedException)
    {
      // index does not exist
      return null;
    }    
  }

  public Task<List<SearchIndex>> ListIndexes(Site site)
  {
    SearchIndexClient client = new
    (
      new(this.Settings.AzureSearchServiceEndpoint),
      new AzureKeyCredential(AzureSearchSettings.Decrypt(site, this.Settings.AzureSearchServiceEncryptedApiKey))
    );

    IEnumerable<SearchIndex> response = client.GetIndexesAsync().ToBlockingEnumerable();

    return Task.FromResult(response.ToList());
  }


  public async Task<Boolean> AddVectorization()
  {
    SearchIndexClient client = await this.SearchIndexClient();

    SearchIndex searchIndex = await client.GetIndexAsync(this.Settings.IndexName);

    searchIndex
      .AddVectorization(this.Site, this.Settings, this.Settings.OpenAIServiceSettings)
      .AddScoringProfiles(this.Settings, true);

    Response<SearchIndex> response = await client.CreateOrUpdateIndexAsync(searchIndex, true);

    return true;
  }


  public async Task<Boolean> IsVectorizationConfigured()
  {
    SearchIndexClient client = await this.SearchIndexClient();

    SearchIndex searchIndex = await client.GetIndexAsync(this.Settings.IndexName);

    return searchIndex.VectorSearch != null;
  }

  public async Task<string> AddSemanticRanking(string name)
  {
    SearchIndexClient client = await this.SearchIndexClient();

    SearchIndex searchIndex = await client.GetIndexAsync(this.Settings.IndexName);

    searchIndex.AddSemanticRanking(name, this.Settings);

    Response<SearchIndex> createIndexResponse = await client.CreateOrUpdateIndexAsync(searchIndex, true);

    return name;
  }

  /// <summary>
  /// Run all indexers that target the selected index.
  /// </summary>
  /// <returns></returns>
  public async Task RunIndexers()
  {
    SearchIndexerClient indexerClient = new
    (
      endpoint: new(this.Settings.AzureSearchServiceEndpoint),
      credential: new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey))
    );

    Response<IReadOnlyList<SearchIndexer>> indexersResponse = await indexerClient.GetIndexersAsync();

    foreach (SearchIndexer indexer in indexersResponse.Value)
    {
      if (indexer.TargetIndexName.Equals(this.Settings.IndexName))
      {
        try
        {
          await RunIndexer(indexer.Name);
        }
        catch (Azure.RequestFailedException ex)
        {
          if (ex.Status == (int)System.Net.HttpStatusCode.Conflict)
          {
            // Another indexer invocation is currently in progress. This is not an error condition: If the indexer is already running, we 
            // don't need to run it.
            this.Logger.LogInformation(ex, "Running Indexer '{name}'.", indexer.Name);
          }
          else
          { 
            // Any other error, log as an error, but continue anyway, a problem running an indexer should not stop the rest of the indexing
            // process.
            this.Logger.LogError(ex, "Running Indexer '{name}'.", indexer.Name);
          }
        }
      }
    }
  }

  public async Task RunIndexer(string name)
  {
    SearchIndexerClient client = new
    (
      endpoint: new(this.Settings.AzureSearchServiceEndpoint),
      credential: new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey))
    );
    Response response = await client.RunIndexerAsync(name);
  }

  /// <summary>
  /// Reset all indexers that target the selected index.
  /// </summary>
  public async Task ResetIndexers()
  {
    SearchIndexerClient indexerClient = new
    (
      endpoint: new(this.Settings.AzureSearchServiceEndpoint),
      credential: new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey))
    );

    Response<IReadOnlyList<SearchIndexer>> indexersResponse = await indexerClient.GetIndexersAsync();

    foreach (SearchIndexer indexer in indexersResponse.Value)
    {
      if (indexer.TargetIndexName.Equals(this.Settings.IndexName))
      {
        await ResetIndexer(indexer.Name);
      }
    }
  }

  public async Task ResetIndexer(string name)
  {
    SearchIndexerClient client = new
    (
      endpoint: new(this.Settings.AzureSearchServiceEndpoint),
      credential: new AzureKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.AzureSearchServiceEncryptedApiKey))
    );

    Response response = await client.ResetIndexerAsync(name);
  }


  public async Task<SearchIndexStatistics> GetIndexSettings()
  {
    SearchIndexClient client = await this.SearchIndexClient();

    // check index
    Response<SearchIndexStatistics> indexResponse = await client.GetIndexStatisticsAsync(this.Settings.IndexName);

    return indexResponse.Value;
  }

  public async Task<Boolean> DeleteIndex()
  {
    SearchIndexClient client = await this.SearchIndexClient();
    await client.GetIndexAsync(this.Settings.IndexName);
    await client.DeleteIndexAsync(this.Settings.IndexName);

    return true;
  }

  public async Task IndexContent(AzureSearchDocument content)
  {
    List<IndexDocumentsAction<AzureSearchDocument>> actions = [];

    // generate vectors for simple content types
    if (this.Settings.UseVectorSearch)
    {
      Azure.AI.OpenAI.AzureOpenAIClient aiClient = new(new(this.Settings.OpenAIServiceSettings.AzureOpenAIEndpoint), new System.ClientModel.ApiKeyCredential(AzureSearchSettings.Decrypt(this.Site, this.Settings.OpenAIServiceSettings.AzureOpenAIEncryptedApiKey)));

      OpenAI.Embeddings.EmbeddingClient embedddingClient = aiClient.GetEmbeddingClient(this.Settings.OpenAIServiceSettings.AzureOpenAIEmbeddingModelDeploymentName);

      if (!String.IsNullOrEmpty(content.Title))
      {
        System.ClientModel.ClientResult<OpenAI.Embeddings.OpenAIEmbedding> titleVectorResponse = await embedddingClient.GenerateEmbeddingAsync(content.Title);
        content.TitleVector = titleVectorResponse.Value.ToFloats().ToArray();
      }

      if (!String.IsNullOrEmpty(content.Summary))
      {
        System.ClientModel.ClientResult<OpenAI.Embeddings.OpenAIEmbedding> summaryVectorResponse = await embedddingClient.GenerateEmbeddingAsync(content.Summary);
        content.SummaryVector = summaryVectorResponse.Value.ToFloats().ToArray();
      }

      if (!String.IsNullOrEmpty(content.Content))
      {
        // generate vector for content / generate chunks      
        await foreach (IndexDocumentsAction<AzureSearchDocument> action in SplitContent(embedddingClient, content))
        {
          actions.Add(action);
        }
      }
    }

    actions.Insert(0, new IndexDocumentsAction<AzureSearchDocument>(IndexActionType.MergeOrUpload, content));

    SearchClient client = await this.GetSearchClient();

    // look for index entries for the same source & sourceId
    await CheckForDuplicates(client, content);
        
    if (String.IsNullOrEmpty(content.Content))
    {
      await EnrichAzureGeneratedEntries(client, content, actions);
    }

    if (actions.Count > 0)
    {
      // execute in batches of 8 so we don't hit size limits
      foreach (IndexDocumentsAction<AzureSearchDocument>[] block in actions.Chunk(8))
      {
        IndexDocumentsBatch<AzureSearchDocument> batch = new();
        foreach (IndexDocumentsAction<AzureSearchDocument> action in block)
        {
          batch.Actions.Add(action);
        }
        await ExecuteBatch(client, batch);
      }

      if (TimeSpan.FromSeconds(this.Settings.IndexingPause) > TimeSpan.Zero)
      {
        await Task.Delay(TimeSpan.FromSeconds(this.Settings.IndexingPause));
      }
    }
  }

  /// <summary>
  /// Search for duplicates by scope and sourceId and delete them.
  /// </summary>
  /// <param name="client"></param>
  /// <param name="content"></param>
  /// <returns></returns>
  private async Task CheckForDuplicates(SearchClient client, AzureSearchDocument content)
  {
    if (!String.IsNullOrEmpty(content.Scope) && !String.IsNullOrEmpty(content.SourceId))
    {
      Response<SearchResults<AzureSearchDocument>> relatedDocumentsResponse = client.Search<AzureSearchDocument>(BuildRelatedItemsQuery(content.SiteId, content.Scope, content.SourceId));

      foreach (SearchResult<AzureSearchDocument> relatedDocument in relatedDocumentsResponse.Value.GetResultsAsync().ToBlockingEnumerable())
      {
        if (content.Url != relatedDocument.Document.Url)
        {
          // handle cases where the Url has changed (like when documents are migrated to Blob storage, or just moved)

          // delete the "old" document from the index. We do not update the index entry when the Url has changed, because the Azure Search "pull"
          // feed will have already created a new entry for the new Url.
          Logger?.LogInformation("Removing replaced index entry '{scope}/{sourceId}'", content.Scope, content.SourceId);
          await RemoveContent(relatedDocument.Document);
        }
        else if (content.Id != relatedDocument.Document.Id)
        {
          // handle cases where we are creating an entry for a scope/sourceId, but there is already an index entry for it, with a different ID, 
          // which would result in duplicate data. This case is unexpected, so we log a warning.
          Logger?.LogWarning("A duplicate index entry for '{scope}/{sourceId}' was detected. The index entry Ids are (this)'{id1}', (other)'{id2}'.", content.Scope, content.SourceId, content.Id, relatedDocument.Document.Id);
        }
      }
    }
  }

  private async Task EnrichAzureGeneratedEntries(SearchClient client, AzureSearchDocument content, List<IndexDocumentsAction<AzureSearchDocument>> actions)
  {
    // for index entries which may be handled by an Azure Search indexer (or some other process),
    // look for related index entries (documents with a ParentId set to our Id) and set meta-data
    // for those index entries. These will typically be index projections ("chunked" index entries).
    try
    {
      Response<SearchResults<AzureSearchDocument>> relatedDocumentsResponse = await client.SearchAsync<AzureSearchDocument>(BuildRelatedItemsQuery(content.Id));

      if (relatedDocumentsResponse.Value != null)
      {
        foreach (SearchResult<AzureSearchDocument> relatedDocument in relatedDocumentsResponse.Value.GetResultsAsync().ToBlockingEnumerable())
        {
          CopyMetaData(content, relatedDocument.Document);
          // prevent overwrite of content field, which is populated by the Azure Search indexer
          relatedDocument.Document.Content = null;

          // extract page number (for chunks) from the Id, which is generated by the Azure Search skill set index projection. Azure
          // search starts page counts at 0, so we +1 
          System.Text.RegularExpressions.Match match = GET_CHUNK_PAGE_ID_REGEX().Match(relatedDocument.Document.Id);
          if (match.Success && int.TryParse(match.Groups[CHUNK_ENTITY_NAME].Value, out int chunkNumber))
          {
            relatedDocument.Document.ChunkNumber = chunkNumber + 1;
            relatedDocument.Document.Title = $"{content.Title} [{chunkNumber + 1}]";
          }
          actions.Add(new IndexDocumentsAction<AzureSearchDocument>(IndexActionType.MergeOrUpload, relatedDocument.Document));
        }
      }
    }
    catch (Exception ex)
    {
      this.Logger.LogError(ex, "Updating meta-data for chunk entries for content of type '{type}', url '{url}'.", content.Type, content.Url);
    }
  }

  private async Task ExecuteBatch(SearchClient client, IndexDocumentsBatch<AzureSearchDocument> batch)
  {
    Response<IndexDocumentsResult> response = await client.IndexDocumentsAsync<AzureSearchDocument>(batch);

    if (response.Value.Results != null)
    {
      foreach (Azure.Search.Documents.Models.IndexingResult result in response.Value.Results)
      {
        if (result.Succeeded)
        {
          Logger.LogTrace("IndexContent for [{key}] succeeded with status '{status}'.", result.Key, result.Status);
        }
        else
        {
          Logger.LogWarning("IndexContent for [{key}] failed with error '{errorMessage}'.", result.Key, result.ErrorMessage);
        }
      }
    }
  }

  private async IAsyncEnumerable<IndexDocumentsAction<AzureSearchDocument>> SplitContent(EmbeddingClient embedddingClient, AzureSearchDocument contentItem)
  {
    if (!String.IsNullOrEmpty(contentItem.Content))
    {
      //string[] tokens = contentItem.Content.Split(WORD_BREAKING_CHARS, StringSplitOptions.RemoveEmptyEntries);
      string[] contentWords = contentItem.Content.Split(WORD_BREAKING_CHARS, StringSplitOptions.RemoveEmptyEntries);
      List<string[]> chunks = contentWords.Chunk(WORDS_PER_CHUNK).ToList();
      int pageCount = chunks.Count(); // (int)Math.Floor((decimal)tokens.Length / WORDS_PER_CHUNK) + 1;

      if (contentWords.Length <= WORDS_PER_CHUNK)
      {
        // document token count is less than the chunk limit, generate vector for the original document instead of generating chunk entries
        try
        {
          this.Logger.LogInformation("The document content fits the vector token limit, generating a vector for the original document content of type '{type},{id}', url '{url}'.", contentItem.Scope, contentItem.SourceId, contentItem.Url);

          contentItem.ContentVector = await BuildVectors(embedddingClient, contentItem.Content);
        }
        catch (System.ClientModel.ClientResultException ex)
        {
          // content is too long (more than 8191 tokens)
          this.Logger.LogError(ex, "A vector for the original document content of type '{type},{id}', url '{url}' was not generated because the content could not be tokenized.", contentItem.Scope, contentItem.SourceId, contentItem.Url);
        }
      }
      else
      {
        this.Logger.LogInformation("The document content exceeds the vector token limit, generating {pagecount} chunked index entries for content of type '{type},{id}', url '{url}'.", pageCount, contentItem.Scope, contentItem.SourceId, contentItem.Url);

        // set .Content property on the main index entry to null - we are chunking because it exceeds the limit. The chunks array already contains the split/chunked content - if
        // we left .Content set, we would get a Request Entity Too Large error response when submitting the index update.
        contentItem.Content = null;

        // generate document chunks
        for (int page = 0; page < pageCount; page++)
        {
          this.Logger.LogTrace("Generating chunk #{page}.", page);

          IEnumerable<string> words = chunks[page].Where(chunk => chunk.Length > 1 && chunk.Length < 30);
          string vectorContent = string.Join(" ", words);
          int tokenCount = words.Count();

          AzureSearchDocument pagedDocument = new();

          CopyMetaData(contentItem, pagedDocument);
          pagedDocument.Content = vectorContent;
          pagedDocument.ChunkNumber = page + 1;

          pagedDocument.Id = $"{contentItem.Id}_{CHUNK_ENTITY_NAME}_{pagedDocument.ChunkNumber}";

          pagedDocument.IndexingDate = DateTime.UtcNow;

          pagedDocument.ParentId = contentItem.Id;
          pagedDocument.Title += $" [{pagedDocument.ChunkNumber}]";
          pagedDocument.IndexingDate = DateTime.UtcNow;

          // only try to generate vectors if the "tokenized" content is not empty
          if (!string.IsNullOrEmpty(vectorContent))
          {
            try
            {
              this.Logger.LogTrace("Building vectors for chunk #{page}.", page);
              pagedDocument.ContentVector = await BuildVectors(embedddingClient, vectorContent);
            }
            catch (System.ClientModel.ClientResultException ex)
            {
              // content is too long (more than 8191 tokens, or some other error from OpenAI)
              this.Logger.LogError(ex, "A vector for chunk #{chunkNumber} for content of type '{type},{id}', url '{url}' was not generated because OpenAI returned an error. Our-end token count was {tokenCount}.", pagedDocument.ChunkNumber, contentItem.Scope, contentItem.SourceId, contentItem.Url, tokenCount);
              this.Logger.LogTrace(ex, "The content which caused the error was '{content}'.", pagedDocument.Content);
            }

            // submit chunked index entry
            if (pagedDocument.ContentVector != null)
            {
              this.Logger.LogTrace("Returning chunk #{page}.", page);
              yield return new IndexDocumentsAction<AzureSearchDocument>(IndexActionType.MergeOrUpload, pagedDocument);
            }
            else
            {
              this.Logger.LogWarning("Skipped saving chunk for content of type '{type},{id}', url '{url}', chunk #{chunkNumber} because a content vector could not be generated.", contentItem.Scope, contentItem.SourceId, contentItem.Url, pagedDocument.ChunkNumber);
            }
          }
        }
      }
    }
  }

  private async Task<float[]> BuildVectors(EmbeddingClient embedddingClient, string value)
  {
    const int MAX_RETRIES = 4;

    if (!String.IsNullOrEmpty(value))
    {
      int retryCount = 0;

      while (retryCount < MAX_RETRIES)
      {
        try
        {
          if (retryCount > 0)
          {
            Logger?.LogTrace("Retry #{retryCount} of {max_retries}", retryCount, MAX_RETRIES);
          }
          System.ClientModel.ClientResult<OpenAIEmbedding> summaryVectorResponse = await embedddingClient.GenerateEmbeddingAsync(value);
          return summaryVectorResponse.Value.ToFloats().ToArray();
        }
        catch (System.ClientModel.ClientResultException ex)
        {
          if (ex.Status == 429)
          {
            // TooManyRequests: The embeddings API is rate limited. Wait and retry
            retryCount++;
            if (retryCount < MAX_RETRIES)
            {
              // wait 3 seconds for the first retry, 6 seconds for the second retry ...
              System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5 * retryCount));
            }
            else
            {
              throw;
            }
          }
          else
          {
            throw;
          }
        }
      }
    }
    return null;
  }


  private static void CopyMetaData(AzureSearchDocument source, AzureSearchDocument target)
  {
    target.SiteId = source.SiteId;
    target.Url = source.Url;

    target.Title = source.Title;
    target.TitleVector = source.TitleVector;

    if (string.IsNullOrEmpty(target.Summary))
    {
      target.Summary = source.Summary;
      target.SummaryVector = source.SummaryVector;
    }

    target.Size = source.Size;
    target.ContentType = source.ContentType;
    target.Categories = source.Categories;
    target.Keywords = source.Keywords;
    target.Language = source.Language;
    target.PublishedDate = source.PublishedDate;
    target.Scope = source.Scope;
    target.SourceId = source.SourceId;
    target.Type = source.Type;

    target.IsSecure = source.IsSecure;
    target.Roles = source.Roles;
  }

  private static SearchOptions BuildRelatedItemsQuery(string id)
  {
    SearchOptions options = new()
    {
      IncludeTotalCount = true,
      QueryType = SearchQueryType.Simple,
      Filter = BuildFilter($"{BuildParentIdFilter(id)}"),
      Skip = 0,
      Size = 1000
    };

    return options;
  }

  private static SearchOptions BuildRelatedItemsQuery(string site, string scope, string sourceId)
  {
    SearchOptions options = new()
    {
      IncludeTotalCount = true,
      QueryType = SearchQueryType.Simple,
      Filter = BuildFilter($"{BuildScopeFilter(site, scope, sourceId)}", $"{BuildExcludeChunkEntriesFilter()}"),
      Skip = 0,
      Size = 1000
    };

    return options;
  }

  private static string BuildScopeFilter(string siteId, string scope, string sourceId)
  {
    // Id.ToString is required here, SearchFilter.Create cannot handle Guids
    return $"{nameof(AzureSearchDocument.SiteId)} eq {SearchFilter.Create($"{siteId}")} and {nameof(AzureSearchDocument.Scope)} eq {SearchFilter.Create($"{scope}")} and {nameof(AzureSearchDocument.SourceId)} eq {SearchFilter.Create($"{sourceId}")}";
  }

  private static string BuildExcludeChunkEntriesFilter()
  {
    return $"{nameof(AzureSearchDocument.ChunkNumber)} eq null";
  }

  public async Task<IndexDocumentsResult> RemoveContent(AzureSearchDocument content)
  {
    SearchClient client = await this.GetSearchClient();
    Response<IndexDocumentsResult> response = await client.DeleteDocumentsAsync<AzureSearchDocument>([content]);
    return response.Value;
  }

  public async Task<Response<SearchResults<AzureSearchDocument>>> QueryIndex(string field, string value)
  {
    string searchTerm = "*";

    SearchOptions searchOptions = new()
    {
      QueryType = SearchQueryType.Simple,
      SearchMode = SearchMode.Any,
      Size = 100
    };

    switch (field)
    {
      case "Term":
        searchTerm = value;
        break;

      default: // all other fields
        searchOptions.Filter = $"{field} eq {SearchFilter.Create($"{value}")}";
        break;
    }

    SearchClient client = await this.GetSearchClient();
    return await client.SearchAsync<AzureSearchDocument>(searchTerm, searchOptions);
  }

  public async Task<Response<SearchResults<AzureSearchDocument>>> QueryIndexByScope(string scope, string sourceId)
  {
    string searchTerm = "*";

    SearchOptions searchOptions = new()
    {
      QueryType = SearchQueryType.Simple,
      SearchMode = SearchMode.Any,
      Size = 100
    };

    searchOptions.Filter = $"{nameof(AzureSearchDocument.Scope)} eq {SearchFilter.Create($"{scope}")} and {nameof(AzureSearchDocument.SourceId)} eq {SearchFilter.Create($"{sourceId}")}";

    SearchClient client = await this.GetSearchClient();
    return await client.SearchAsync<AzureSearchDocument>(searchTerm, searchOptions);
  }

  public async Task<AzureSearchDocument> GetContentByKey(string key)
  {
    SearchClient client = await this.GetSearchClient();
    AzureSearchDocument response = await client.GetDocumentAsync<AzureSearchDocument>(key);
    return response;
  }

  public async Task<Response<SearchResults<AzureSearchDocument>>> Search(SearchQuery query)
  {
    SearchClient client = await this.GetSearchClient();

    // https://learn.microsoft.com/en-us/dotnet/api/azure.search.documents.searchoptions.querytype?view=azure-dotnet
    SearchOptions searchOptions = new()
    {
      HighlightPreTag = query.Options.HasFlag(SearchQuery.QueryOptions.Highlight) ? "<em>" : "",
      HighlightPostTag = query.Options.HasFlag(SearchQuery.QueryOptions.Highlight) ? "</em>" : "",
      IncludeTotalCount = true,
      ScoringStatistics = ScoringStatistics.Global,
      QueryType = String.IsNullOrEmpty(this.Settings.SemanticConfigurationName) ? SearchQueryType.Simple : SearchQueryType.Semantic,
      SearchMode = query.StrictSearchTerms ? SearchMode.All : SearchMode.Any,
      Size = query.PagingSettings.PageSize,
      Skip = query.PagingSettings.FirstRowIndex,
      Filter = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}"),
    };

    if (!String.IsNullOrEmpty(this.Settings.SemanticConfigurationName))
    {
      searchOptions.SemanticSearch = new()
      {
        SemanticConfigurationName = this.Settings.SemanticConfigurationName
      };

      if (searchOptions.QueryType == SearchQueryType.Semantic)
      {
        searchOptions.SemanticSearch.QueryCaption = new(QueryCaptionType.Extractive);
        searchOptions.SemanticSearch.QueryAnswer = new(QueryAnswerType.Extractive) { Count = 3 };
        searchOptions.SemanticSearch.ErrorMode = SemanticErrorMode.Partial;
      }
    }

    if (this.Settings.UseVectorSearch)
    {
      searchOptions.VectorSearch = new()
      {
        FilterMode = VectorFilterMode.PostFilter
      };
      VectorizableTextQuery vectorQuery = new(query.SearchTerm)
      {
        // https://learn.microsoft.com/en-us/azure/search/vector-search-how-to-query?tabs=query-2024-07-01%2Cbuiltin-portal#number-of-ranked-results-in-a-vector-query-response
        KNearestNeighborsCount = 10, // default is 50
        Weight = 2,
        Exhaustive = true,
        Threshold = new VectorSimilarityThreshold(0.5) // https://community.openai.com/t/rule-of-thumb-cosine-similarity-thresholds/693670
      };
      vectorQuery.Fields.Add(nameof(AzureSearchDocument.TitleVector));
      vectorQuery.Fields.Add(nameof(AzureSearchDocument.SummaryVector));
      vectorQuery.Fields.Add(nameof(AzureSearchDocument.ContentVector));

      searchOptions.VectorSearch.Queries.Add(vectorQuery);
    }

    AddRange(searchOptions.SearchFields, BuildSearchFields());
    AddRange(searchOptions.Select, BuildSelectFields());
    AddRange(searchOptions.HighlightFields, BuildHighlightFields(query));

    if (query.SearchTerm.Length > 100)
    {
      // Azure Search has a 100 character limit for the search term
      query.SearchTerm = query.SearchTerm[..100];
    }

    return await client.SearchAsync<AzureSearchDocument>(query.SearchTerm, searchOptions);
  }

  public async Task<Response<SuggestResults<AzureSearchDocument>>> Suggest(SearchQuery query)
  {
    SearchClient client = await this.GetSearchClient();

    SuggestOptions searchOptions = new()
    {
      Size = query.PagingSettings.PageSize,
      Filter = BuildFilter(BuildSiteFilter(query), BuildRolesFilter(query), BuildScopeFilter(query), BuildArgsFilter(query)),
      UseFuzzyMatching = true
    };

    AddRange(searchOptions.SearchFields, BuildSuggesterFields());
    AddRange(searchOptions.Select, BuildSelectFields());

    if (query.SearchTerm.Length > 100)
    {
      // Azure Search has a 100 character limit for the search term
      query.SearchTerm = query.SearchTerm[..100];
    }

    Response<SuggestResults<AzureSearchDocument>> result = await client.SuggestAsync<AzureSearchDocument>(query.SearchTerm, IndexExtensions.SUGGESTER_NAME, searchOptions);

    return result;
  }

  private static void AddRange(ICollection<string> list, IEnumerable<string> values)
  {
    foreach (string value in values)
    {
      list.Add(value);
    }
  }

  private static string BuildFilter(params string[] values)
  {
    // filter null values, add spaces in between filter expressions
    return String.Join(" and ", values.Where(value => !String.IsNullOrEmpty(value)));
  }

  private static List<string> BuildSearchFields()
  {
    return
    [
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Summary),
      nameof(AzureSearchDocument.Content),
      nameof(AzureSearchDocument.Keywords),
      nameof(AzureSearchDocument.Categories)
    ];
  }

  private static List<string> BuildSuggesterFields()
  {
    return
    [
      nameof(AzureSearchDocument.Title)
    ];
  }

  private static List<string> BuildSelectFields()
  {
    return
    [
      nameof(AzureSearchDocument.SiteId),
      nameof(AzureSearchDocument.Url),
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Summary),
      nameof(AzureSearchDocument.Scope),
      nameof(AzureSearchDocument.Type),
      nameof(AzureSearchDocument.SourceId),
      nameof(AzureSearchDocument.ContentType),
      nameof(AzureSearchDocument.PublishedDate),
      nameof(AzureSearchDocument.IndexingDate),
      nameof(AzureSearchDocument.Size),
      nameof(AzureSearchDocument.Keywords),
      nameof(AzureSearchDocument.Categories),
      nameof(AzureSearchDocument.Roles)
    ];
  }

  private static List<string> BuildHighlightFields(SearchQuery query)
  {
    return
    [
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Summary),
      nameof(AzureSearchDocument.Content)
    ];
  }


  // from: https://learn.microsoft.com/en-us/azure/search/hybrid-search-overview
  // Explicit sort orders override relevanced-ranked results, so if you want similarity and BM25 relevance, omit
  // sorting in your query."
  ////private List<string> BuildOrderBy(SearchQuery query)
  ////{
  ////  return
  ////  [
  ////    "search.score() desc"
  ////  ];
  ////}

  private static string BuildSiteFilter(SearchQuery query)
  {
    // Id.ToString is required here.
    // IDE0071 "thinks" that string interpolation can be simplified by removing .ToString, but the parameter to Search.Create
    // is a FormattableString object rather than a "regular" interpolated string, and SearchFilter.Create doesn't know how to
    // deal with Guids.
#pragma warning disable IDE0071 // Simplify interpolation
    return $"{nameof(AzureSearchDocument.SiteId)} eq {SearchFilter.Create($"{query.Site.Id.ToString()}")}";
#pragma warning restore IDE0071 // Simplify interpolation
  }

  private static string BuildParentIdFilter(string parentId)
  {
    // Id.ToString is required here, SearchFilter.Create cannot handle Guids
    return $"{nameof(AzureSearchDocument.ParentId)} eq {SearchFilter.Create($"{parentId}")}";
  }

  //private static string BuildPageNumberFilter(SearchQuery query)
  //{
  //  // We exclude "page 0" from search results, because it is a repeat of the main document (index entry)
  //  return $"{nameof(AzureSearchDocument.PageNumber)} ne 1";
  //}

  private static string BuildRolesFilter(SearchQuery query)
  {
    // https://learn.microsoft.com/en-us/azure/search/search-query-odata-search-in-function
    if (query.Roles?.Any() == true)
    {
      // Id.ToString is required here, SearchFilter.Create cannot handle Guids
      string values = SearchFilter.Create($"{String.Join(",", query.Roles.Select(role => role.Id.ToString()))}");
      return $"({nameof(AzureSearchDocument.Roles)}/any(g: search.in(g, {values})) or {nameof(AzureSearchDocument.IsSecure)} eq false)";
    }
    else
    {
      return null;
    }
  }

  private static string BuildScopeFilter(SearchQuery query)
  {
    string result = "";

    if (query.IncludedScopes.Count != 0)
    {
      string values = SearchFilter.Create($"{String.Join(",", query.IncludedScopes.Select(scope => scope))}");
      result += $"search.in({nameof(AzureSearchDocument.Scope)}, {values}, ',')";
    }

    if (query.ExcludedScopes.Count != 0)
    {
      string values = SearchFilter.Create($"{String.Join(",", query.ExcludedScopes.Select(scope => scope))}");
      string result2 = $"(search.in({nameof(AzureSearchDocument.Scope)}, {values}, ',') eq false)";

      if (!String.IsNullOrEmpty(result))
      {
        result = $"({result} and {result2})";
      }
      else
      {
        result = result2;
      }
    }

    return String.IsNullOrEmpty(result) ? null : result;
  }

  private static string BuildArgsFilter(SearchQuery query)
  {
    // future use
    return null;
  }
}

