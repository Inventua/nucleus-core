using System;
using System.Collections.Generic;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.AzureSearch.ViewModels;

public class Settings : AzureSearchSettings
{
  //public const string DUMMY_APIKEY = "!@#NOT_CHANGED^&*";

  // This constructor is used by model binding
  public Settings() { }

  public Settings(Site site) : base(site)
  {
    if (String.IsNullOrEmpty(base.AzureSearchServiceEncryptedApiKey))
    {
      this.AzureSearchApiKey = "";
    }

    if (String.IsNullOrEmpty(base.AzureSearchServiceEncryptedApiKey))
    {
      this.AzureOpenAIApiKey = "";
    }
  }

  public string NewIndexName { get; set; }

  public string NewSemanticRankingConfigurationName { get; set; }

  public string AzureSearchApiKey { get; set; } = DUMMY_APIKEY;
  public string AzureOpenAIApiKey { get; set; } = DUMMY_APIKEY;

  public List<string> SearchIndexes { get; set; }

  //public List<string> Indexers { get; set; }

  public List<DataSource> DataSources { get; set; }

  public List<string> Semanticonfigurations { get; set; }

  public class DataSource 
  {    
    public string FileSystemProviderKey { get; set; }
    public string FileSystemProviderName { get; set; }

    public string IndexerName { get; set; }

    public DataSource() { }

    public DataSource(FileSystemProviderInfo provider, string indexerName)
    {
      this.FileSystemProviderKey = provider.Key;
      this.FileSystemProviderName = provider.Name;
      this.IndexerName = indexerName;
    }
  }
}
