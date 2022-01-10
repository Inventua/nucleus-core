using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Extensions.ElasticSearch
{
	public class ConfigSettings
	{
		internal const string SITESETTING_SERVER_URL = "elasticsearch:server-url";
		internal const string SITESETTING_INDEX_NAME = "elasticsearch:indexname";
		internal const string SITESETTING_ATTACHMENT_MAXSIZE = "elasticsearch:attachment-maxsize";

		internal const string SITESETTING_BOOST_TITLE = "elasticsearch:boost-title";
		internal const string SITESETTING_BOOST_SUMMARY = "elasticsearch:boost-summary";
		internal const string SITESETTING_BOOST_CATEGORIES = "elasticsearch:boost-categories";
		internal const string SITESETTING_BOOST_KEYWORDS = "elasticsearch:boost-keywords";
		internal const string SITESETTING_BOOST_CONTENT = "elasticsearch:boost-content";

		internal const string SITESETTING_BOOST_ATTACHMENT_AUTHOR = "elasticsearch:boost-attachment-author";
		internal const string SITESETTING_BOOST_ATTACHMENT_KEYWORDS = "elasticsearch:boost-attachment-keywords";
		internal const string SITESETTING_BOOST_ATTACHMENT_NAME = "elasticsearch:boost-attachment-name";
		internal const string SITESETTING_BOOST_ATTACHMENT_TITLE = "elasticsearch:boost-attachment-title";
		

		public string ServerUrl { get; set; }
		public string IndexName { get; set; }
		public int AttachmentMaxSize { get; set; }

		public Nucleus.Abstractions.Search.SearchQuery.BoostSettings Boost { get; set; } = new();

		// This constructor is used by model binding
		public ConfigSettings() { }

		public ConfigSettings(Site site)
		{			
			if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_SERVER_URL, out string serverUrl))
			{
				this.ServerUrl = serverUrl;
			}

			if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_INDEX_NAME, out string indexName))
			{
				this.IndexName = indexName;
			}

			if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_ATTACHMENT_MAXSIZE, out string maxSize))
			{
				if (int.TryParse(maxSize, out int attachmentMaxSize))
				{
					this.AttachmentMaxSize = attachmentMaxSize;
				}
			}

			this.Boost.Title = GetSetting(site, SITESETTING_BOOST_TITLE, this.Boost.Title);
			this.Boost.Summary = GetSetting(site, SITESETTING_BOOST_SUMMARY, this.Boost.Summary);
			this.Boost.Categories = GetSetting(site, SITESETTING_BOOST_CATEGORIES, this.Boost.Categories);
			this.Boost.Keywords = GetSetting(site, SITESETTING_BOOST_KEYWORDS, this.Boost.Keywords);
			this.Boost.Content = GetSetting(site, SITESETTING_BOOST_CONTENT, this.Boost.Content);

			this.Boost.AttachmentAuthor = GetSetting(site, SITESETTING_BOOST_ATTACHMENT_AUTHOR, this.Boost.AttachmentAuthor);
			this.Boost.AttachmentKeywords = GetSetting(site, SITESETTING_BOOST_ATTACHMENT_KEYWORDS, this.Boost.AttachmentKeywords);
			this.Boost.AttachmentName = GetSetting(site, SITESETTING_BOOST_ATTACHMENT_NAME, this.Boost.AttachmentName);
			this.Boost.AttachmentTitle = GetSetting(site, SITESETTING_BOOST_ATTACHMENT_TITLE, this.Boost.AttachmentTitle);

		}

		private double GetSetting(Site site, string key, double defaultValue)
		{
			if (site.SiteSettings.TryGetValue(key, out string value))
			{
				if (double.TryParse(value, out double parsedValue))
				{
					return parsedValue;
				}
			}
			return defaultValue;
		}
	}
}

