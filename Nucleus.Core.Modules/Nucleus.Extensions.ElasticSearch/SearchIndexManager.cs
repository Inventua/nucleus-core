using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.ElasticSearch
{
	// https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/elasticsearch-net-getting-started.html

	public class SearchIndexManager : ISearchIndexManager
	{		
    private ILogger<SearchIndexManager> Logger { get; }
    private ElasticSearchRequest _request { get; set; }

    public SearchIndexManager (ILogger<SearchIndexManager> logger)
		{
			this.Logger = logger;
		}

    private ElasticSearchRequest Request(Site site)
    {
      ConfigSettings settings = new(site);

      if (String.IsNullOrEmpty(settings.ServerUrl))
      {
        throw new InvalidOperationException($"The Elastic search server url is not set for site '{site.Name}'.");
      }

      if (String.IsNullOrEmpty(settings.IndexName))
      {
        throw new InvalidOperationException($"The Elastic search index name is not set for site '{site.Name}'.");
      }

      if (_request == null || !_request.Equals(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(site, settings.EncryptedPassword), settings.CertificateThumbprint))
      { 
        _request = new(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(site, settings.EncryptedPassword), settings.CertificateThumbprint, TimeSpan.FromSeconds(settings.IndexingPause));      
      }
            
      return _request;
    }

    public async Task<Boolean> CanConnect(Site site)
    {
      if (site == null)
      {
        throw new NullReferenceException("site must not be null.");
      }

      return await this.Request(site).Connect();
    }

		public async Task ClearIndex(Site site)
		{
			if (site == null)
			{
				throw new NullReferenceException("site must not be null.");
			}

			await this.Request(site).DeleteIndex();
		}

    private List<ContentMetaData> queue = new();

		public async Task Index(ContentMetaData metadata)
		{
			if (metadata.Site == null)
			{
				throw new NullReferenceException("metaData.Site must not be null.");
			}

			ConfigSettings settings = new(metadata.Site);
      			
      ElasticSearchDocument document = new(metadata, settings);
			Nest.IndexResponse response = await this.Request(metadata.Site).IndexContent(document);

			// free up memory - file content is part of the feed data, and this could exhaust available memory 
			document.Dispose();
		}

		public async Task Remove(ContentMetaData metaData)
		{
			ConfigSettings settings = new(metaData.Site);			
			await this.Request(metaData.Site).RemoveContent(new ElasticSearchDocument(metaData, settings));
		}
	}
}
