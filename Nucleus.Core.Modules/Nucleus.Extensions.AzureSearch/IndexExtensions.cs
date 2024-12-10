using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Search.Documents.Indexes.Models;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.AzureSearch.Models;

namespace Nucleus.Extensions.AzureSearch;

internal static class IndexExtensions
{
  public const string SUGGESTER_NAME = "suggest-autocomplete";

  public const string VECTORIZER_NAME = "openai-vectorizer";
  public const string VECTORIZER_MODEL_NAME = "text-embedding-3-small";

  public const string VECTOR_HNSW_PROFILE = "vector-profile-hnsw";
  public const string VECTOR_HNSW_CONFIG_NAME = "vector-config-hnsw";

  public const string VECTOR_EXHAUSTIVEKNN_PROFILE = "vector-profile-exhaustive-knn";
  public const string VECTOR_EXHAUSTIVEKNN_CONFIG = "vector-config-exhaustive-knn";

  public const int VECTOR_DIMENSIONS = 1536;
  public const int AVG_TOKEN_SIZE_CHARS = 8;
  public const int SPLITTER_OVERLAP_TOKENS = 100;

  // the Azure search limit for chunk sizes is 8000. We use a smaller value because in testing, we got errors like "skill
  // input text was 8603 tokens, which is greater than the maximum allowed '8000'...". This may be a bug in Azure Search, or
  // it could be because we have set pageOverlapLength to a non-zero value & Azure search doesn't count those towards the limit, 
  // or something along those lines. Either way, a smaller chunk size fixes the issue.
  public const int SPLITTER_CHUNK_SIZE = 2000;

  public const int SPLITTER_OVERLAP = SPLITTER_OVERLAP_TOKENS * AVG_TOKEN_SIZE_CHARS;

  public const string SCORING_PROFILE_METADATA = "metadata-only";
  public const string SCORING_PROFILE_VECTORS = "metadata-and-vectors";

  public static SearchIndex AddSuggesters(this SearchIndex searchIndex, AzureSearchServiceSettings settings)
  {
    if (!searchIndex.Suggesters.Any())
    {
      searchIndex.Suggesters.Add(new SearchSuggester(SUGGESTER_NAME, BuildSuggesterFields()));
    }       

    return searchIndex;
  }
   
  public static SearchIndex AddScoringProfiles(this SearchIndex searchIndex, AzureSearchServiceSettings settings, Boolean useVectors)
  {
    searchIndex.AddScoringProfile(SCORING_PROFILE_METADATA, new()
    {
      { nameof(AzureSearchDocument.Title), 2 },
      { nameof(AzureSearchDocument.Summary), 1 },
      { nameof(AzureSearchDocument.Content), 1 },
      { nameof(AzureSearchDocument.Keywords), 1.5 },

      { nameof(AzureSearchDocument.Categories), 1.5 }
    });

    if (useVectors)
    {
      searchIndex.AddScoringProfile(SCORING_PROFILE_VECTORS, new()
      {
        { nameof(AzureSearchDocument.Title), 2 },
        { nameof(AzureSearchDocument.Summary), 1 },
        { nameof(AzureSearchDocument.Content), 1 },
        { nameof(AzureSearchDocument.Keywords), 1.5 },

        { nameof(AzureSearchDocument.Categories), 1.5 },

        { nameof(AzureSearchDocument.ContentVector), 2 },
        { nameof(AzureSearchDocument.SummaryVector), 2 },
        { nameof(AzureSearchDocument.TitleVector), 2 }
      });
    }

    searchIndex.DefaultScoringProfile = searchIndex.ScoringProfiles.Last().Name;
    
    return searchIndex;
  }

  private static void AddScoringProfile(this SearchIndex searchIndex, string name, Dictionary<string, double> weights)
  {
    if (!ScoringProfileExists(searchIndex, name))
    {
      searchIndex.ScoringProfiles.Add(new ScoringProfile(name)
      {
        TextWeights = new(weights)
      });
    }
  }

  private static Boolean ScoringProfileExists(this SearchIndex searchIndex, string name)
  {
    return searchIndex.ScoringProfiles?.Any(profile => profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) == true;
  }

  public static SearchIndex AddSemanticRanking(this SearchIndex searchIndex, string name, AzureSearchServiceSettings settings)
  {
    searchIndex.SemanticSearch = new SemanticSearch()
    {
      Configurations =
        {
          new SemanticConfiguration(name, new SemanticPrioritizedFields()
          {
            TitleField = new SemanticField(nameof(AzureSearchDocument.Title)),
            ContentFields =
            {
              new SemanticField(nameof(AzureSearchDocument.Summary)),
              new SemanticField(nameof(AzureSearchDocument.Content))              
            },
            KeywordsFields =
            {
              new SemanticField(nameof(AzureSearchDocument.Keywords)),
              new SemanticField(nameof(AzureSearchDocument.Categories))
            }
          })
        }
    };

    settings.SemanticConfigurationName = name;

    return searchIndex;
  }

  public static SearchIndex AddVectorization(this SearchIndex searchIndex, Site site, AzureSearchServiceSettings searchSettings, OpenAIServiceSettings openAIServiceSettings)
  {
    searchIndex.VectorSearch = new VectorSearch()
    {
      Profiles =
        {
          new VectorSearchProfile(VECTOR_HNSW_PROFILE, VECTOR_HNSW_CONFIG_NAME)
          {
            VectorizerName = VECTORIZER_NAME,
          },
          new VectorSearchProfile(VECTOR_EXHAUSTIVEKNN_PROFILE, VECTOR_EXHAUSTIVEKNN_CONFIG)
        },
      Algorithms =
        {
            new HnswAlgorithmConfiguration(VECTOR_HNSW_CONFIG_NAME),
            new ExhaustiveKnnAlgorithmConfiguration(VECTOR_EXHAUSTIVEKNN_CONFIG)
        },
      Vectorizers =
        {
          new AzureOpenAIVectorizer(VECTORIZER_NAME)
          {
            Parameters = new AzureOpenAIVectorizerParameters()
            {
              ResourceUri = new Uri(openAIServiceSettings.AzureOpenAIEndpoint),
              ApiKey = AzureSearchSettings.Decrypt(site, openAIServiceSettings.AzureOpenAIEncryptedApiKey),
              DeploymentName = openAIServiceSettings.AzureOpenAIEmbeddingModelDeploymentName,
              ModelName = VECTORIZER_MODEL_NAME
            }
          }
        }
    };

    AddVectorField(searchIndex, nameof(AzureSearchDocument.TitleVector));
    AddVectorField(searchIndex, nameof(AzureSearchDocument.SummaryVector));
    AddVectorField(searchIndex, nameof(AzureSearchDocument.ContentVector));

    return searchIndex;
  }

  /// <summary>
  /// Add a vector field to the index, if it is not already present.
  /// </summary>
  /// <param name="searchIndex"></param>
  /// <param name="fieldName"></param>
  private static void AddVectorField(SearchIndex searchIndex, string fieldName)
  {
    if (!searchIndex.Fields.Any(field => field.Name == fieldName))
    {
      searchIndex.Fields.Add(new SearchField(fieldName, SearchFieldDataType.Collection(SearchFieldDataType.Single))
      {
        IsSearchable = true,
        VectorSearchDimensions = VECTOR_DIMENSIONS,
        VectorSearchProfileName = VECTOR_HNSW_PROFILE
      });
    }
  }

  public static List<string> BuildSuggesterFields()
  {
    // The AutoCompleteValues field is used because suggesters can only contain fields that use the default analyzer and the keywords/category fields
    // use the keyword analyzer because they require an exact match).
    return new List<string>()
      {
        nameof(AzureSearchDocument.Title),
        nameof(AzureSearchDocument.Summary),
        nameof(AzureSearchDocument.Content),
        nameof(AzureSearchDocument.AutoCompleteValues)
      };
  }
}
