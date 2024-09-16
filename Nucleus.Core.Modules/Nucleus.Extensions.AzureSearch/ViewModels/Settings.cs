using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.AzureSearch.ViewModels;

public class Settings : ConfigSettings
{
  public const string DUMMY_APIKEY = "!@#NOT_CHANGED^&*";

  // This constructor is used by model binding
  public Settings() { }

  public Settings(Site site) : base(site)
  {
    if (String.IsNullOrEmpty(base.EncryptedApiKey))
    {
      this.ApiKey = "";
    }

    if (String.IsNullOrEmpty(base.EncryptedAzureOpenAIApiKey))
    {
      this.AzureOpenAIApiKey = "";
    }
  }

  public string ApiKey { get; set; } = DUMMY_APIKEY;
  public string AzureOpenAIApiKey { get; set; } = DUMMY_APIKEY;

  public List<string> Indexers { get; set; }

  public List<string> Semanticonfigurations { get; set; }

}
