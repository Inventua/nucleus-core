using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Nucleus.Extensions.AzureSearch.Models;

public class OpenAIServiceSettings 
{
  /// <summary>
  /// Constant string used as alternative for encrypted API key.
  /// </summary>
  public const string DUMMY_APIKEY = "!@#NOT_CHANGED^&*";

  [Display(Name = "Azure OpenAI Endpoint")]
  public string AzureOpenAIEndpoint { get; set; }

  public string AzureOpenAIEncryptedApiKey { get; set; }

  [Display(Name = "Azure OpenAI Embedding Model Deployment Name")]
  public string AzureOpenAIEmbeddingModelDeploymentName { get; set; }

}
