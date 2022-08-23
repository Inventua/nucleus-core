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

		public void Index(ContentMetaData metadata)
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
			Nest.IndexResponse response = request.IndexContent(document);

			// free up memory - file content is part of the feed data, and this can exhaust available memory 
			document.Dispose();
		}

		public void Remove(ContentMetaData metadata)
		{
			ConfigSettings settings = new(metadata.Site);

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(metadata.Site, settings.EncryptedPassword), settings.CertificateThumbprint);

			request.RemoveContent(new ElasticSearchDocument(metadata));
		}
	}
}
