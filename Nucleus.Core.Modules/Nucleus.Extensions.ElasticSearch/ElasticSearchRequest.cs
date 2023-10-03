using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;
using Elasticsearch.Net;
using Nucleus.Abstractions.Search;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.ElasticSearch
{
	public class ElasticSearchRequest
	{
		public Uri Uri { get; }
		public string IndexName { get; }

		private string Username { get; }
		private string Password { get; }
		private string CertificateThumbprint { get; }


		private ElasticClient _client { get; set; }

		public string DebugInformation { get; internal set; }

		private const string NOATTACHMENT_PIPELINE = "no-attachment-pipeline";
		private const string ATTACHMENT_PIPELINE = "attachment-pipeline";

		public ElasticSearchRequest(Uri uri, string indexName, string username, string password, string certificateThumbprint)
		{
			this.Uri = uri;
			this.IndexName = indexName.ToLower();  // elastic search indexes must be lower case
			this.Username = username;
			this.Password = password;
			this.CertificateThumbprint = certificateThumbprint;
		}

		public async Task<ElasticClient> GetClient()
		{			
			if (this._client == null)
			{
				if (!await Connect())
				{
					throw new InvalidOperationException(this.DebugInformation);
				}
			}
			return this._client;			
		}

		public async Task<bool> Connect()
		{
			SingleNodeConnectionPool connectionPool = new SingleNodeConnectionPool(this.Uri);
			Nest.ConnectionSettings connectionSettings = new Nest.ConnectionSettings(connectionPool);
			PingResponse pingResponse;
			CreateIndexResponse createIndexResponse;
			PutMappingResponse mapResponse;
			PutPipelineResponse attachmentPipelineResponse;
			PutPipelineResponse noAttachmentPipelineResponse;

			connectionSettings.DefaultIndex(this.IndexName.ToLower());    // index name must be lowercase
			connectionSettings.DisableDirectStreaming(true);          // enables debug info
			connectionSettings.RequestTimeout(new TimeSpan(0, 0, 30));
			connectionSettings.EnableApiVersioningHeader();

			if (!String.IsNullOrEmpty(this.Username))
			{
				connectionSettings.BasicAuthentication(this.Username, this.Password);
			}

			if (!String.IsNullOrEmpty(this.CertificateThumbprint))
			{
				connectionSettings.CertificateFingerprint(this.CertificateThumbprint);
			}

			ElasticClient client = new(connectionSettings);

			pingResponse = await client.PingAsync(BuildPingRequest());
			if (pingResponse.IsValid)
			{
				// Create index
				if (!(await client.Indices.ExistsAsync(this.IndexName)).Exists)
				{
					createIndexResponse = await client.Indices.CreateAsync(BuildIndexRequest(this.IndexName));
					if (!createIndexResponse.IsValid)
					{
						this.DebugInformation = createIndexResponse.DebugInformation;
						throw createIndexResponse.OriginalException;
					}

					mapResponse = await client.MapAsync<ElasticSearchDocument>(e => e.AutoMap());
					if (!mapResponse.IsValid)
					{
						this.DebugInformation = mapResponse.DebugInformation;
						throw createIndexResponse.OriginalException;
					}

					// Configure a pipeline for attachments 
					attachmentPipelineResponse = await ConfigureAttachmentPipeline(client);
					if (!attachmentPipelineResponse.IsValid)
					{
						this.DebugInformation = attachmentPipelineResponse.DebugInformation;
						throw createIndexResponse.OriginalException;
					}

					noAttachmentPipelineResponse = await ConfigureNoAttachmentPipeline(client);
					if (!noAttachmentPipelineResponse.IsValid)
					{
						this.DebugInformation = noAttachmentPipelineResponse.DebugInformation;
						throw createIndexResponse.OriginalException;
					}
				}
			}
			else
			{
				this.DebugInformation = pingResponse.DebugInformation;
				throw pingResponse.OriginalException;
			}

			this._client = client;
			return true;
		}

		private Nest.PingRequest BuildPingRequest()
		{
			return new Nest.PingRequest();
		}

		private Nest.CreateIndexRequest BuildIndexRequest(string Index)
		{
			return new Nest.CreateIndexRequest(Index, BuildIndexState());
		}

		private IIndexState BuildIndexState()
		{
			return new IndexState() { };
		}

		private async Task<PutPipelineResponse> ConfigureAttachmentPipeline(ElasticClient client)
		{
			PutPipelineResponse response;
			PutPipelineRequest request;

			// Creates a pipeline request with the processors 
			// - AttachmentProcessor that ingests base64 encoded (content) field and stores in [attachment] as Nest.Attachment field.
			// - RemoveProcessor that removes the content field after ingesting it.
			request = new PutPipelineRequest(ATTACHMENT_PIPELINE)
			{
				Description = "Document attachment pipeline",
				Processors = new List<IProcessor>()
				{
					new AttachmentProcessor()
					{
						Field = ParseField(nameof(ElasticSearchDocument.Content)),
						TargetField = ParseField(nameof(ElasticSearchDocument.Attachment)),
						IndexedCharacters = -1,
						IgnoreMissing = true,
						OnFailure = new List<IProcessor>()
						{
							new SetProcessor()
							{
								Field = ParseField(nameof(ElasticSearchDocument.Status)),
								Value = "Error: could not process attachment. Ingest processor error: {{_ingest.on_failure_message}}."
							},
							new RemoveProcessor()
							{
								// Elastic search failed to consume the value of the .Content property (to populate the index), but we still
								// want to remove the original value, as it is not useful, and could be large.  Also security - don't store the
								// original outside nucleus.
								Field =  ParseField(nameof(ElasticSearchDocument.Content))
							}
						}
					},
					new SetProcessor()
					{
						Field = ParseField(nameof(ElasticSearchDocument.FeedProcessingDateTime)),
            Value = "{{{_ingest.timestamp}}}"
          },
					new SetProcessor()
					{
						Field = ParseField(nameof(ElasticSearchDocument.Status)),
						Value = "Entry with attachment was processed successfully."
					},
					//new SetProcessor()
					//{
					//	// If the entry did not have a summary, and the attachment has a title, set the summary to the attachment title
					//	// TODO: this does not work
					//	Field = ParseField(nameof(ElasticSearchDocument.Summary)),
					//	Value = $"{{{{{{{ParseField(nameof(ElasticSearchDocument.Attachment))}.{ParseField(nameof(ElasticSearchDocument.Attachment.Title))}}}}}}}",
					//	If = $"{ParseField(nameof(ElasticSearchDocument.Summary))} == null",
					//	IgnoreEmptyValue = true,
					//	OnFailure = new List<IProcessor>()
					//	{
					//		new SetProcessor()
					//		{
					//			Field = ParseField(nameof(ElasticSearchDocument.Status)),
					//			Value = "Error: could not process attachment. Ingest processor error: {{_ingest.on_failure_message}}." 
					//		}
					//	}
					//},
					//new SetProcessor()
					//{
					//	// If the entry does not have keywords, and the attachment has keywords, set the entry keywords to the attachment keywords
					//	Field = ParseField(nameof(ElasticSearchDocument.Keywords)),
					//	Value = $"{{{{{{{ParseField(nameof(ElasticSearchDocument.Attachment))}.{ParseField(nameof(ElasticSearchDocument.Attachment.Keywords))}}}}}}}",
					//	If = $"{ParseField(nameof(ElasticSearchDocument.Keywords))} == null",
					//	IgnoreEmptyValue = true
					//},
					new SetProcessor()
					{
						Field = ParseField(nameof(ElasticSearchDocument.SuggesterTitle)),
						Value = "{{{name}}}"
					},
					new RemoveProcessor()
					{
						// Once Elastic search has consumed the value of the .Content property (to populate the index), remove the original
						// value, as it is not useful, and could be large.  Also security - don't store the original outside nucleus.
						Field = ParseField(nameof(ElasticSearchDocument.Content))
					}
				}
			};

			response = await client.Ingest.PutPipelineAsync(request);

			return response;
		}

		private async Task<PutPipelineResponse> ConfigureNoAttachmentPipeline(ElasticClient client)
		{
			PutPipelineResponse objResponse;
			PutPipelineRequest objRequest;

			objRequest = new PutPipelineRequest(NOATTACHMENT_PIPELINE)
			{
				Description = "document-noattachment",
				Processors = new List<IProcessor>()
				{
					new SetProcessor()
					{
						Field = ParseField(nameof(ElasticSearchDocument.Status)),
						Value = "Entry was processed successfully (no attachment)."
					},
					new SetProcessor()
					{
							Field = ParseField(nameof(ElasticSearchDocument.FeedProcessingDateTime)),
              Value = "{{{_ingest.timestamp}}}"
          },
					new SetProcessor()
					{
							Field = ParseField(nameof(ElasticSearchDocument.SuggesterTitle)),
							Value = "{{{name}}}"
					},
					new RemoveProcessor()
					{
						// remove the original content value, as it is not useful, and could be large.  Also security - don't store the
						// original outside nucleus.
						Field =  ParseField(nameof(ElasticSearchDocument.Content))
					}
				}
			};

			objResponse = await client.Ingest.PutPipelineAsync(objRequest);

			return objResponse;
		}

		public async Task<long> CountIndex()
		{
			ElasticClient client = await GetClient();
			return (await client.CountAsync<ElasticSearchDocument>()).Count;
		}


		public async Task<bool> DeleteIndex()
		{
			Nest.DeleteIndexResponse objIndexResponse;
			ElasticClient client = await GetClient();

			objIndexResponse = await client.Indices.DeleteAsync(this.IndexName);
      
      this.DebugInformation = objIndexResponse.DebugInformation;
      
			return objIndexResponse.IsValid;
		}

		public async Task<Nest.UpdateIndexSettingsResponse> ResetIndexSettings()
		{
			Nest.IUpdateIndexSettingsRequest objIndexRequest;
			Nest.UpdateIndexSettingsResponse objIndexResponse;
			ElasticClient client = await GetClient();

			objIndexRequest = new UpdateIndexSettingsRequest(this.IndexName);

			objIndexRequest.IndexSettings = new DynamicIndexSettings();

			objIndexRequest.IndexSettings.Add("index.blocks.read_only_allow_delete", false);

			objIndexResponse = await client.Indices.UpdateSettingsAsync(objIndexRequest);

      this.DebugInformation = objIndexResponse.DebugInformation;

      return objIndexResponse;
		}

		public async Task<GetIndexSettingsResponse> GetIndexSettings()
		{
			GetIndexSettingsRequest objRequest = new GetIndexSettingsRequest();
			ElasticClient client = await GetClient();

			objRequest.Human = true;
			objRequest.IncludeDefaults = true;
			objRequest.Pretty = true;

			return await client.Indices.GetSettingsAsync(new GetIndexSettingsRequest());
		}

		public async Task<Nest.IndexResponse> IndexContent(ElasticSearchDocument content)
		{
			RequestConfiguration requestSettings = new RequestConfiguration();
			Nest.IndexRequest<ElasticSearchDocument> indexRequest;
			Nest.IndexResponse indexResponse;
			ElasticClient client = await GetClient();

			indexRequest = new IndexRequest<ElasticSearchDocument>(content)
			{
				Pipeline = !(String.IsNullOrEmpty(content.Content)) && IsSupportedType(content.ContentType) ? ATTACHMENT_PIPELINE : NOATTACHMENT_PIPELINE
			};

			requestSettings.DisableDirectStreaming = false;
			requestSettings.MaxRetries = 5;
			requestSettings.RequestTimeout = new TimeSpan(0, 15, 0);

			indexRequest.RequestConfiguration = requestSettings;
			indexResponse = await client.IndexAsync<ElasticSearchDocument>(indexRequest);

			if (!indexResponse.IsValid)
			{
				throw indexResponse.OriginalException;
			}
			return indexResponse;
		}

		public async Task<Nest.DeleteResponse> RemoveContent(ElasticSearchDocument content)
		{
			RequestConfiguration requestSettings = new RequestConfiguration();
			Nest.DeleteRequest<ElasticSearchDocument> deleteRequest;
			Nest.DeleteResponse deleteResponse;
			ElasticClient client = await GetClient();

			deleteRequest = new DeleteRequest<ElasticSearchDocument>(content)
			{
			};

			requestSettings.DisableDirectStreaming = false;
			requestSettings.MaxRetries = 5;
			requestSettings.RequestTimeout = new TimeSpan(0, 15, 0);

			deleteRequest.RequestConfiguration = requestSettings;
			deleteResponse = await client.DeleteAsync<ElasticSearchDocument>(content);

			if (!deleteResponse.IsValid)
			{
				throw deleteResponse.OriginalException;
			}
			return deleteResponse;
		}

		private Boolean IsSupportedType(string contentType)
		{
			string[] supportedTypes =
			{
				"application/pdf",
				"text/plain",
				"text/html",
				"application/msword",
				"application/vnd.openxmlformats-officedocument.wordprocessingml.document"
			};

			return supportedTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
		}

		public async Task<ISearchResponse<ElasticSearchDocument>> Suggest(SearchQuery query)
		{
			SearchRequest searchRequest;
			ISearchResponse<ElasticSearchDocument> response;
			SuggestContainer suggest = new SuggestContainer();

			suggest.Add("suggest-title", BuildSuggestBucket(query.SearchTerm, ParseField(nameof(ElasticSearchDocument.SuggesterTitle)), query.PagingSettings.PageSize));

			searchRequest = new SearchRequest<ElasticSearchDocument>(this.IndexName)
			{
				Query = BuildSearchQuery(query),
				Source = new SourceFilter 
				{ 
					Includes = "*", 
					Excludes = new List<string>() 
					{ 
						ParseField(nameof(ElasticSearchDocument.Content)), 
						ParseField(nameof(ElasticSearchDocument.Attachment)),
						ParseField(nameof(ElasticSearchDocument.Roles)) 
					}.ToArray() 
				},
				Suggest = suggest,
				Size = query.PagingSettings.PageSize,
				From = query.PagingSettings.FirstRowIndex,
				PostFilter = BuildSiteFilter(query) & BuildRolesFilter(query) & BuildArgsFilter(query) & BuildScopeFilter(query),
				Sort = new List<ISort>()
				{
					new FieldSort()
					{
						Order = SortOrder.Descending,
						Field = "_score"
					}
				}
			};

			response = await Search(searchRequest);

			return response;
		}

		private SuggestBucket BuildSuggestBucket(string searchTerm, string fieldName, int maxSuggestions)
		{
			return new SuggestBucket()
			{
				Text = searchTerm,
				Completion = new CompletionSuggester()
				{
					Field = fieldName,
					SkipDuplicates = true,
					Size = maxSuggestions,
					Fuzzy = new SuggestFuzziness()
					{
						MinLength = 4,
						Fuzziness = Fuzziness.EditDistance(2)
					}
				}
			};
		}


		public async Task<ISearchResponse<ElasticSearchDocument>> Search(SearchQuery query)
		{
			SearchRequest searchRequest;

			if (query.SearchTerm == string.Empty)
				throw new ApplicationException("No search term");
			else
				searchRequest = new SearchRequest(this.IndexName)
				{
					Query = BuildSearchQuery(query),
					Source = new SourceFilter
					{
						Includes = "*",
						Excludes = new List<string>()
						{
							ParseField(nameof(ElasticSearchDocument.Attachment)),
							ParseField(nameof(ElasticSearchDocument.Roles))
						}.ToArray()
					},
					Size = query.PagingSettings.PageSize,
					From = query.PagingSettings.FirstRowIndex,
					Highlight = BuildHighlighter(),
					PostFilter = BuildSiteFilter(query) & BuildRolesFilter(query) & BuildArgsFilter(query) & BuildScopeFilter(query),
					Sort = BuildSortFilter(query)
				};

			// .Pretty = Me.DebugMode,
			// .Explain = Me.DebugMode,
			// .Human = Me.DebugMode,
			ISearchResponse<ElasticSearchDocument> response = await Search(searchRequest);

			if (response.IsValid)
			{
				return ReplaceHighlights(response);
			}
			else
			{
				throw new InvalidOperationException(response.ServerError.Error.Reason);
			}
		}

		private async Task<ISearchResponse<ElasticSearchDocument>> Search(Nest.SearchRequest searchRequest)
		{
			ElasticClient client = await GetClient();
			return await client.SearchAsync<ElasticSearchDocument>(searchRequest);
		}

		private ISearchResponse<ElasticSearchDocument> ReplaceHighlights(ISearchResponse<ElasticSearchDocument> response)
		{
			// replace document field values with highlighted ones
			foreach (IHit<ElasticSearchDocument> objHit in response.Hits)
			{
				objHit.Source.Score = objHit.Score;

				foreach (KeyValuePair<string, IReadOnlyCollection<string>> objHighlight in objHit.Highlight)
				{
					string strField = objHighlight.Key;

					foreach (string strHiglightItem in objHighlight.Value) // objHighlight.Value.Highlights
					{
						switch (strField)
						{
							case "title":
								{
									objHit.Source.Title = strHiglightItem;
									break;
								}
							case "summary":
								{
									objHit.Source.Summary = strHiglightItem;
									break;
								}
						}
					}
				}
			}

			return response;
		}

		private IHighlight BuildHighlighter()
		{
			return new Highlight()
			{
				PreTags = new string[] { ("<em>") },
				PostTags = new string[] { ("</em>") },
				Fields = new Dictionary<Field, IHighlightField>()
				{
					{ nameof(ElasticSearchDocument.Summary), new HighlightField() { RequireFieldMatch = false } },
					{ nameof(ElasticSearchDocument.Title), new HighlightField() { RequireFieldMatch = false } }
				}
			};
		}

		private Nest.QueryContainer BuildSearchQuery(SearchQuery query)
		{
			Nest.QueryContainer searchContainer = new QueryContainer(new SimpleQueryStringQuery()
			{
				Fields = SearchFields(query),
				Query = query.SearchTerm.Trim() + (query.SearchTerm.Trim().Contains(' ') ? "" : "*"),
				DefaultOperator = query.StrictSearchTerms ? Operator.And : Operator.Or,
				AnalyzeWildcard = true
			});

			return new Nest.BoolQuery()
			{
				Must = new QueryContainer[] { searchContainer }
			};
		}

		private Fields SearchFields(SearchQuery query)
		{
			return Infer.Field<ElasticSearchDocument>(doc => doc.Title, query.Boost?.Title)
				.And(Infer.Field<ElasticSearchDocument>(doc => doc.Summary, query.Boost?.Summary))
				.And(Infer.Field<ElasticSearchDocument>(doc => doc.Categories, query.Boost?.Categories))
				.And(Infer.Field<ElasticSearchDocument>(doc => doc.Keywords, query.Boost?.Keywords))
				.And(Infer.Field<ElasticSearchDocument>(doc => doc.Attachment.Author, query.Boost?.AttachmentAuthor))
				.And(Infer.Field<ElasticSearchDocument>(doc => doc.Attachment.Keywords, query.Boost?.AttachmentKeywords))
				.And(Infer.Field<ElasticSearchDocument>(doc => doc.Attachment.Name, query.Boost?.AttachmentName))
				.And(Infer.Field<ElasticSearchDocument>(doc => doc.Attachment.Title, query.Boost?.AttachmentTitle))
				.And(Infer.Field<ElasticSearchDocument>(doc => doc.Attachment.Content, query.Boost?.Content));
		}

		private Nest.QueryContainer BuildRolesFilter(SearchQuery query)
		{
			Nest.QueryContainer objRolesContainer = null;

			if (query.Roles?.Any() == true)
			{
				foreach (Role role in query.Roles)
				{
					objRolesContainer = objRolesContainer | BuildTermQuery(nameof(ElasticSearchDocument.Roles), role.Id.ToString());
				}

				return new BoolQuery()
				{
					Must = new QueryContainer[]
					{
						new BoolQuery()
						{
								Should = new QueryContainer[] { objRolesContainer, BuildTermQuery(nameof(ElasticSearchDocument.IsSecure), "false") }
						}
					}
				};
			}
			else
			{
				return null;
			}
		}

		private Nest.QueryContainer BuildSiteFilter(SearchQuery query)
		{
			return BuildTermQuery(ParseField(nameof(ElasticSearchDocument.SiteId)), query.Site.Id.ToString());
		}

		private Nest.QueryContainer BuildScopeFilter(SearchQuery query)
		{
			Nest.BoolQuery result = new();

			if (query.IncludedScopes.Any())
			{
				result.Must = new QueryContainer[] { BuildTermsQuery(nameof(ElasticSearchDocument.Scope), query.IncludedScopes) };
			}

			if (query.ExcludedScopes.Any())
			{
				result.MustNot = new QueryContainer[] { BuildTermsQuery(nameof(ElasticSearchDocument.Scope), query.ExcludedScopes) };				
			}

			return result;
		}

		private Nest.QueryContainer BuildArgsFilter(SearchQuery query)
		{
			// when we add more criteria to the SearchQuery class, this function will add code to filter based
			// on selections
			return null;
		}

		private List<ISort> BuildSortFilter(SearchQuery query)
		{
			return new List<ISort>()
			{
				new FieldSort()
				{
					Order = SortOrder.Descending,
					Field = "_score"
				}
			};
		}


		private Nest.QueryContainer BuildTermQuery(string fieldName, string value)
		{
			return new TermQuery()
			{
				Field = ParseField(fieldName),
				Value = value
			};
		}

		private Nest.QueryContainer BuildTermsQuery(string fieldName, List<string> values)
		{
			return new TermsQuery()
			{
				Field = ParseField(fieldName),
				Terms =  values				
			};
		}

		private string ParseField(string Value)
		{
			return Value.Substring(0, 1).ToLower() + Value.Substring(1);
		}


	}
}