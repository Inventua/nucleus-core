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

		public Nucleus.Abstractions.Models.Sitemap.Sitemap SiteMap (Site site)
		{
			string filename = GetFilename(this.Options, site);
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
			string filename = GetFilename(this.Options, site);

			if (System.IO.File.Exists(filename))
			{
				System.IO.File.Delete(filename);
			}

			return Task.CompletedTask;
		}

		public Task Index(ContentMetaData metadata)
		{
			Abstractions.Models.Sitemap.Sitemap siteMap = this.SiteMap(metadata.Site);
			SiteMapEntry item = siteMap.Items.Where(item => item.Url == metadata.Url).FirstOrDefault();

			if (item == null)
			{
				item = new();
				siteMap.Items.Add(item);
			}

			item.Url = metadata.Url;

			this.Write(metadata.Site, siteMap);

			return Task.CompletedTask;
		}

		public Task Remove(ContentMetaData metadata)
		{
			Abstractions.Models.Sitemap.Sitemap siteMap = this.SiteMap(metadata.Site);
			SiteMapEntry item = siteMap.Items.Where(item => item.Url == metadata.Url).FirstOrDefault();

			if (item != null)
			{
				siteMap.Items.Remove(item);
			}

			Write(metadata.Site, siteMap);

			return Task.CompletedTask;
		}

		private static string GenerateValidPath (string value)
		{
			foreach (char character in System.IO.Path.GetInvalidPathChars().Concat(System.IO.Path.GetInvalidFileNameChars()).Concat(new char[] { '.' }))
			{
				value = value.Replace(character, ' ');
			}

			value = value.Replace("  ", " ");

			if (value.Length > 64)
			{
				value = value.Substring(0, 64);	
			}

			return value;
		}

		public static string GetFilename(Abstractions.Models.Configuration.FolderOptions options, Site site)
		{
			string path = System.IO.Path.Join(options.GetCacheFolder(true), "SiteMap");
			path = System.IO.Path.Combine(path, GenerateValidPath(site.Name));
			options.EnsureExists(path);
			return System.IO.Path.Join(path, "Sitemap.xml");
		}

		private void Write(Site site, Abstractions.Models.Sitemap.Sitemap siteMap)
		{
			string filename = GetFilename(this.Options, site);
			
			using (System.IO.Stream output = System.IO.File.OpenWrite(filename))
			{
				System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Sitemap.Sitemap));
				serializer.Serialize(output, siteMap);
			}
		}
	}
}
