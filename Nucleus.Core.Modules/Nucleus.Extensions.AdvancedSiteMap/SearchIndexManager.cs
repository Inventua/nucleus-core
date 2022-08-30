using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Sitemap;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AdvancedSiteMap
{
	public class SearchIndexManager : ISearchIndexManager
	{
		private Nucleus.Abstractions.Models.Configuration.FolderOptions Options { get; }

		public SearchIndexManager (IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> options)
		{
			this.Options = options.Value;
		}

		public Nucleus.Abstractions.Models.Sitemap.Sitemap SiteMap ()
		{
			string filename = GetFilename();
			if (!System.IO.File.Exists(filename))
			{
				return new();
			}
			else
			{
				using (System.IO.Stream input = System.IO.File.OpenRead(filename))
				{
					System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Sitemap.Sitemap));
					return serializer.Deserialize(input) as Nucleus.Abstractions.Models.Sitemap.Sitemap;
				}		
			}
		}

		public Task ClearIndex(Site site)
		{
			return Task.CompletedTask;
		}

		public Task Index(ContentMetaData metadata)
		{
			Abstractions.Models.Sitemap.Sitemap siteMap = this.SiteMap();
			SiteMapEntry item = siteMap.Items.Where(item => item.Url == metadata.Url).FirstOrDefault();

			if (item == null)
			{
				item = new();
				siteMap.Items.Add(item);
			}

			item.Url = metadata.Url;

			this.Write(siteMap);

			return Task.CompletedTask;
		}

		public Task Remove(ContentMetaData metadata)
		{
			Abstractions.Models.Sitemap.Sitemap siteMap = this.SiteMap();
			SiteMapEntry item = siteMap.Items.Where(item => item.Url == metadata.Url).FirstOrDefault();

			if (item != null)
			{
				siteMap.Items.Remove(item);
			}

			Write(siteMap);

			return Task.CompletedTask;
		}

		private string GetFilename()
		{
			string path = System.IO.Path.Join(this.Options.GetTempFolder(true), "SiteMap");
			this.Options.EnsureExists(path);
			return System.IO.Path.Join(path, "Sitemap.xml");
		}

		private void Write(Abstractions.Models.Sitemap.Sitemap siteMap)
		{
			string filename = GetFilename();
			
			using (System.IO.Stream output = System.IO.File.OpenWrite(filename))
			{
				System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Sitemap.Sitemap));
				serializer.Serialize(output, siteMap);
			}
		}
	}
}
