using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.ElasticSearch
{
	// https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/elasticsearch-net-getting-started.html

	public class SearchIndexManager : ISearchIndexManager
	{		
		public SearchIndexManager ()
		{
						
		}

    public async Task<Boolean> CanConnect(Site site)
    {
      if (site == null)
      {
        throw new NullReferenceException("site must not be null.");
      }

      ConfigSettings settings = new(site);

      if (String.IsNullOrEmpty(settings.ServerUrl))
      {
        throw new InvalidOperationException($"The Elastic search server url is not set for site '{site.Name}'.");
      }

      if (String.IsNullOrEmpty(settings.IndexName))
      {
        throw new InvalidOperationException($"The Elastic search index name is not set for site '{site.Name}'.");
      }

      ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(site, settings.EncryptedPassword), settings.CertificateThumbprint);
      return await request.Connect();
    }

		public async Task ClearIndex(Site site)
		{
			if (site == null)
			{
				throw new NullReferenceException("site must not be null.");
			}

			ConfigSettings settings = new(site);

			if (String.IsNullOrEmpty(settings.ServerUrl))
			{
				throw new InvalidOperationException($"The Elastic search server url is not set for site '{site.Name}', index not cleared.");
			}

			if (String.IsNullOrEmpty(settings.IndexName))
			{
				throw new InvalidOperationException($"The Elastic search index name is not set for site '{site.Name}', index not cleared.");
			}

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(site, settings.EncryptedPassword), settings.CertificateThumbprint);
			await request.DeleteIndex();
		}

		public async Task Index(ContentMetaData metadata)
		{
			if (metadata.Site == null)
			{
				throw new NullReferenceException("metaData.Site must not be null.");
			}

			ConfigSettings settings = new(metadata.Site);

			if (String.IsNullOrEmpty(settings.ServerUrl))
			{
				throw new InvalidOperationException($"The Elastic search server url is not set for site '{metadata.Site.Name}', content not indexed.");
			}

			if (String.IsNullOrEmpty(settings.IndexName))
			{
				throw new InvalidOperationException($"The Elastic search index name is not set for site '{metadata.Site.Name}', content not indexed.");
			}

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(metadata.Site, settings.EncryptedPassword), settings.CertificateThumbprint);

			ElasticSearchDocument document = new(metadata);
			Nest.IndexResponse response = await request.IndexContent(document);

			// free up memory - file content is part of the feed data, and this can exhaust available memory 
			document.Dispose();
		}

		public async Task Remove(ContentMetaData metadata)
		{
			ConfigSettings settings = new(metadata.Site);

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(metadata.Site, settings.EncryptedPassword), settings.CertificateThumbprint);

			await request.RemoveContent(new ElasticSearchDocument(metadata));
		}
	}
}
