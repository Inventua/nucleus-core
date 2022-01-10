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
				throw new ArgumentException($"The site parameter is required.", nameof(metadata.Site));
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

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName);

			Nest.IndexResponse response = request.IndexContent(new ElasticSearchDocument(metadata));			

		}

		public void Remove(ContentMetaData metadata)
		{
			ConfigSettings settings = new(metadata.Site);

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName);

			request.RemoveContent(new ElasticSearchDocument(metadata));
		}
	}
}
