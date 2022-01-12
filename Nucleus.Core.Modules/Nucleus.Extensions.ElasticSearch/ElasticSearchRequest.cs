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

		private ElasticClient _client { get; set; }

		public string DebugInformation { get; internal set; }

		private const string NOATTACHMENT_PIPELINE = "no-attachment-pipeline";
		private const string ATTACHMENT_PIPELINE = "attachment-pipeline";

		public ElasticSearchRequest(Uri uri, string indexName)
		{
			this.Uri = uri;
			this.IndexName = indexName.ToLower();  // elastic search indexes must be lower case
		}

		public ElasticClient Client
		{
			get
			{
				if (this._client == null)
				{
					if (!Connect())
					{
						throw new InvalidOperationException(this.DebugInformation);
					}
				}
				return this._client;
			}
			set
			{
				this._client = value;
			}
		}

		private bool Connect()
		{
			SingleNodeConnectionPool connectionPool = new SingleNodeConnectionPool(this.Uri);
			Nest.ConnectionSettings connectionSettings = new Nest.ConnectionSettings(connectionPool);
			CreateIndexResponse createIndexResponse;
			PutMappingResponse mapResponse;
			PutPipelineResponse attachmentPipelineResponse;
			PutPipelineResponse noAttachmentPipelineResponse;

			connectionSettings.DefaultIndex(this.IndexName.ToLower());    // index name must be lowercase
			connectionSettings.DisableDirectStreaming(true);          // enables debug info
			connectionSettings.RequestTimeout(new TimeSpan(0, 0, 30));

			this.Client = new ElasticClient(connectionSettings);

			// Create index
			if (!this.IndexExists())
			{
				createIndexResponse = this.Client.Indices.Create(BuildIndexRequest(this.IndexName));
				if (!createIndexResponse.IsValid)
				{
					this.DebugInformation = createIndexResponse.DebugInformation;
					return false;
				}

				mapResponse = this.Client.Map<ElasticSearchDocument>(e => e.AutoMap());
				if (!mapResponse.IsValid)
				{
					this.DebugInformation = mapResponse.DebugInformation;
					return false;
				}

				// Configure a pipeline for attachments 
				attachmentPipelineResponse = ConfigureAttachmentPipeline();
				if (!attachmentPipelineResponse.IsValid)
				{
					this.DebugInformation = attachmentPipelineResponse.DebugInformation;
					return false;
				}

				noAttachmentPipelineResponse = ConfigureNoAttachmentPipeline();
				if (!noAttachmentPipelineResponse.IsValid)
				{
					this.DebugInformation = noAttachmentPipelineResponse.DebugInformation;
					return false;
				}
			}

			return true;
		}

		private Nest.CreateIndexRequest BuildIndexRequest(string Index)
		{
			return new Nest.CreateIndexRequest(Index, BuildIndexState());
		}

		private IIndexState BuildIndexState()
		{
			return new IndexState() { };
		}

		private PutPipelineResponse ConfigureAttachmentPipeline()
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

			response = this.Client.Ingest.PutPipeline(request);

			return response;
		}

		private PutPipelineResponse ConfigureNoAttachmentPipeline()
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
							Value = DateTime.Now.ToString()
					},
					new SetProcessor()
					{
							Field = ParseField(nameof(ElasticSearchDocument.SuggesterTitle)),
							Value = "{{{name}}}"
					},
					new RemoveProcessor()
					{
						// Elastic search failed to consume the value of the .Content property (to populate the index), but we still
						// want to remove the original value, as it is not useful, and could be large.  Also security - don't store the
						// original outside nucleus.
						Field =  ParseField(nameof(ElasticSearchDocument.Content))
					}
				}
			};

			objResponse = this.Client.Ingest.PutPipeline(objRequest);

			return objResponse;
		}

		public bool IndexExists()
		{
			return this.Client.Indices.Exists(this.IndexName).Exists;
		}

		public long CountIndex()
		{
			return this.Client.Count<ElasticSearchDocument>().Count;
		}


		public bool DeleteIndex()
		{
			Nest.DeleteIndexResponse objIndexResponse;

			objIndexResponse = this.Client.Indices.Delete(this.IndexName);

			return objIndexResponse.IsValid;
		}

		public Nest.UpdateIndexSettingsResponse ResetIndexSettings()
		{
			Nest.IUpdateIndexSettingsRequest objIndexRequest;
			Nest.UpdateIndexSettingsResponse objIndexResponse;

			objIndexRequest = new UpdateIndexSettingsRequest(this.IndexName);

			objIndexRequest.IndexSettings = new DynamicIndexSettings();

			objIndexRequest.IndexSettings.Add("index.blocks.read_only_allow_delete", false);

			objIndexResponse = this.Client.Indices.UpdateSettings(objIndexRequest);

			return objIndexResponse;
		}

		public GetIndexSettingsResponse GetIndexSettings()
		{
			GetIndexSettingsRequest objRequest = new GetIndexSettingsRequest();

			objRequest.Human = true;
			objRequest.IncludeDefaults = true;
			objRequest.Pretty = true;

			return this.Client.Indices.GetSettings(new GetIndexSettingsRequest());
		}

		public Nest.IndexResponse IndexContent(ElasticSearchDocument content)
		{
			RequestConfiguration requestSettings = new RequestConfiguration();
			Nest.IndexRequest<ElasticSearchDocument> indexRequest;
			Nest.IndexResponse indexResponse;

			indexRequest = new IndexRequest<ElasticSearchDocument>(content)
			{
				Pipeline = !(String.IsNullOrEmpty(content.Content)) && IsSupportedType(content.ContentType) ? ATTACHMENT_PIPELINE : NOATTACHMENT_PIPELINE
			};

			requestSettings.DisableDirectStreaming = false;
			requestSettings.MaxRetries = 5;
			requestSettings.RequestTimeout = new TimeSpan(0, 15, 0);

			indexRequest.RequestConfiguration = requestSettings;
			indexResponse = this.Client.Index<ElasticSearchDocument>(indexRequest);

			if (!indexResponse.IsValid)
			{
				throw indexResponse.OriginalException;
			}
			return indexResponse;
		}

		public Nest.DeleteResponse RemoveContent(ElasticSearchDocument content)
		{
			RequestConfiguration requestSettings = new RequestConfiguration();
			Nest.DeleteRequest<ElasticSearchDocument> deleteRequest;
			Nest.DeleteResponse deleteResponse;

			deleteRequest = new DeleteRequest<ElasticSearchDocument>(content)
			{				 
			};

			requestSettings.DisableDirectStreaming = false;
			requestSettings.MaxRetries = 5;
			requestSettings.RequestTimeout = new TimeSpan(0, 15, 0);

			deleteRequest.RequestConfiguration = requestSettings;
			deleteResponse = this.Client.Delete<ElasticSearchDocument>(content);

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

		public ISearchResponse<ElasticSearchDocument> Suggest(SearchQuery query, int maxSuggestions)
		{
			SearchRequest searchRequest;
			ISearchResponse<ElasticSearchDocument> response;
			SuggestContainer suggest = new SuggestContainer();

			suggest.Add("suggest-title", BuildSuggestBucket(query.SearchTerm, ParseField(nameof(ElasticSearchDocument.SuggesterTitle)), maxSuggestions));
			
			searchRequest = new SearchRequest(this.IndexName)
			{
				Query = BuildSearchQuery(query),
				Source = new SourceFilter { Includes = "*", Excludes = new List<string>() { nameof(ElasticSearchDocument.Attachment), nameof(ElasticSearchDocument.Roles) }.ToArray() },
				Suggest = suggest,
				PostFilter = BuildSiteFilter(query) & BuildRolesFilter(query) & BuildArgsFilter(query),
				Sort = new List<ISort>()
				{
					new FieldSort()
					{
						Order = SortOrder.Descending,
						Field = "_score"
					}
				}
			};
			
			response = Search(searchRequest);

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


		public ISearchResponse<ElasticSearchDocument> Search(SearchQuery query)
		{
			SearchRequest searchRequest;
			//SourceFilter fieldFilter;

			if (query.SearchTerm == string.Empty)
				throw new ApplicationException("No search term");
			else				
				searchRequest = new SearchRequest(this.IndexName)
				{
					Query = BuildSearchQuery(query),
					Source = new SourceFilter { Includes = "*", Excludes = new List<string>() { nameof(ElasticSearchDocument.Attachment), nameof(ElasticSearchDocument.Roles) }.ToArray() },
					Size = query.PagingSettings.PageSize,
					From = (query.PagingSettings.CurrentPageIndex - 1) * query.PagingSettings.PageSize,
					Highlight = BuildHighlighter(),
					PostFilter = BuildSiteFilter(query) & BuildRolesFilter(query) & BuildArgsFilter(query),
					Sort = BuildSortFilter(query)
				};

			// .Pretty = Me.DebugMode,
			// .Explain = Me.DebugMode,
			// .Human = Me.DebugMode,
			ISearchResponse<ElasticSearchDocument> response = Search(searchRequest);

			if (response.IsValid)
			{
				return ReplaceHighlights(response);
			}
			else
			{
				throw new InvalidOperationException(response.ServerError.Error.Reason);
			}
		}

		private ISearchResponse<ElasticSearchDocument> Search(Nest.SearchRequest searchRequest)
		{
			return this.Client.Search<ElasticSearchDocument>(searchRequest);
		}

		public ISearchResponse<ElasticSearchDocument> ReplaceHighlights(ISearchResponse<ElasticSearchDocument> response)
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
					{	nameof(ElasticSearchDocument.Summary), new HighlightField() { RequireFieldMatch = false }	},
					{ nameof(ElasticSearchDocument.Title), new HighlightField()	{	RequireFieldMatch = false	}	}
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

		private string ParseField(string Value)
		{
			return Value.Substring(0, 1).ToLower() + Value.Substring(1);
		}


	}
}