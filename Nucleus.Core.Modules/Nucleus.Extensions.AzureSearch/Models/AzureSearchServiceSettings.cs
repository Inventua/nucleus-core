using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Nucleus.Extensions.AzureSearch.Models;

public class AzureSearchServiceSettings 
{
  /// <summary>
  /// Constant string used as alternative for encrypted API key.
  /// </summary>
  public const string DUMMY_APIKEY = "!@#NOT_CHANGED^&*";

  [Display(Name = "Azure Search Service Endpoint")]
  public string AzureSearchServiceEndpoint { get; set; }

  public string AzureSearchServiceEncryptedApiKey { get; set; }

  [Display(Name = "Azure Search Index Name")]
  public string IndexName { get; set; }
    
  [Display(Name = "Azure Search Semantic Configuration Name")]
  public string SemanticConfigurationName { get; set; }

  [Display(Name = "Use Vector Search?")]
  public Boolean UseVectorSearch { get; set; }

  public int AttachmentMaxSize { get; set; } = 32;
    
  public double IndexingPause { get; set; } = 1;

}
