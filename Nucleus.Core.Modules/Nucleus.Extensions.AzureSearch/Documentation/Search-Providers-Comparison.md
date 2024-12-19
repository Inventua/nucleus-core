## Nucleus Search Providers Comparison

|                           | Open Source/Free | Keyword Search  | Vector/Semantic Search   | Semantic Ranking       | File Content Extraction |
|---------------------------|------------------|-----------------|---------------------------------------------------| ------------------------|
| Built-in Basic Search     | Yes              | Yes             | No                       | No                     | No                      |
| Elastic Search            | Yes [2]          | Yes             | No [1]                   | No [2]                 | Yes                     |
| Azure Search              | No               | Yes             | Yes [4]                  | Yes [4]                | Yes                     |
| Typesense                 | Yes [6]          | Yes             | Yes                      | No                     | Yes [5]                 |
| Bing Custom Search        | No [3]           | No              | No                       | No                     | No                      |
| Google Custom Search      | No [3]           | No              | No                       | No                     | No                      |

[1] The open source version of Elastic Search has a vector search feature, but does not have a feature to generate vector values during indexing. The 
cloud and paid editions support vector generation and semantic search, but this feature is not implemented in the Nucleus search provider.  
[2] Elastic Search has a open-source edition, as well as paid options. Not all features are available in the open source edition.  Elastic Search has a
semantic re-ranking feature in 'techical preview' in version 8.15. Semantic re-ranking requires Vector/Semantic Search, which is not available in the open-source edition, and is not implemented in the Nucleus search provider.  
[3] Bing and Google custom search search provide a limited number of free transactions, but are paid services.  
[4] This feature is implemented in the Azure Search provider, but there are additional costs (Azure) and it can be disabled.  
[5] Requires a Tika server, but document content extraction using a Tika server is integrated into the Nucleus search index manager for Typesense.  
[6] Typesense has a paid cloud-based service, but the open source edition supports all of the features of the cloud version.  

### Basic Search Provider
The built-in Basic Search provider does not implement an indexer, it uses SQL to query the pages and files tables. It is present so that the search functions 
in the file manager and page manager can work without installing another search provider. It can be used with the search module, but users can only do keyword
searches on page name, title, description and keywords, module titles and file names.

### Azure Search
The [Nucleus search provider for Azure Search](/other-extensions/azure-search/) is the most fully-featured provider, but the 
[Azure Search service](https://learn.microsoft.com/en-us/azure/search/search-what-is-azure-search) is the most expensive. The Semantic Ranking 
and Vectorization/Vector search features require additional Azure services (and therefore cost more). 

### Elastic Search
The [Nucleus search provider for Elastic Search](/other-extensions/elastic-search/) only implements keyword searching. We may implement support for 
vector and sematic search in the future. 

### Typesense
The [Nucleus search provider for Typesense](/other-extensions/typesense-search/) is the second-most fully-featured implementation. If you plan to self-manage 
your search service, you can install Typesense and Tika server and get nearly all of the features of the more expensive cloud-based services. If you are self-hosting,
be aware that generating vectors using the gte-large model during indexing is very slow.  Indexing runs in a scheduled task so the slow indexing performance is 
generally not noticable. When you are populating your index for the first time, you may need to wait a while before all resources are added to your search index.
> Our test/development Typesense server is an old Mac Mini i5 (2 core) running Unbuntu, which works, but is not adequate for production use. A faster server and GPU would 
reduce or eliminate the vector-generation performance issue.

### Bing and Google
The [Nucleus search provider for Bing](/other-extensions/bing-custom-search/) and [Nucleus search provider for Google](/other-extensions/google-custom-search/) 
use the indexes which are created and managed by each service, so the Nucleus providers do not have an index manager component. The other search providers 
for Nucleus can store meta-data in the search index (like published date, resource type and size, keywords and categories), but the Bing and Google services 
do not have this capability.

## Retrieval Augmented Generation
The Azure OpenAI chat completions service can integrate with your Azure Search index to implement 
[Retrieval-Augmented Generation](https://learn.microsoft.com/en-us/azure/search/retrieval-augmented-generation-overview) (RAG). We have developed a 
proof-of-concept module which provides a chat completion interface using an Azure Search index.

Elastic Search and Typesense also have support for Retrieval Augmented Generation (but our proof-of-concept was for Azure Search only). 

**Elastic Search**: Our current implementation of the Elastic Search provider does not implement vectors, which is normally a requirement for RAG. 

**Typesense**: Typesense can integrate with OpenAI (that is, [OpenAI](https://openai.com/), not Microsoft Azure OpenAI as at December 2024) for generative AI, but we have not 
tested RAG with Typesense.