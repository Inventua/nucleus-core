using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Sitemap;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AdvancedSiteMap;

[DisplayName("Advanced Site Map")]
public class SearchIndexManager : ISearchIndexManager
{
  private Nucleus.Abstractions.Models.Configuration.FolderOptions Options { get; }
  private ILogger<SearchIndexManager> Logger { get; }
  private static SemaphoreSlim _fileAccessSemaphore { get; } = new(1);

  public SearchIndexManager(IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> options, ILogger<SearchIndexManager> logger)
  {
    this.Options = options.Value;
    this.Logger = logger;
  }

  public Nucleus.Abstractions.Models.Sitemap.Sitemap SiteMap(Site site)
  {
    string filename = GetFilename(this.Options, site);
    if (!System.IO.File.Exists(filename))
    {
      return new();
    }
    else
    {
      try
      {
        _fileAccessSemaphore.Wait();
        using (System.IO.Stream input = System.IO.File.OpenRead(filename))
        {
          System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Sitemap.Sitemap));
          return serializer.Deserialize(input) as Nucleus.Abstractions.Models.Sitemap.Sitemap;
        }
      }
      catch(System.InvalidOperationException ex)
      {
        // if there is a deserialization error, create a new file
        this.Logger.LogError(ex, "Error reading {filename}.  A new file was generated.", filename);
        return new();
      }
      finally
      {
        _fileAccessSemaphore.Release();
      }
    }
  }

  public Task ClearIndex(Site site)
  {
    string filename = GetFilename(this.Options, site);

    if (System.IO.File.Exists(filename))
    {
      try
      {
        _fileAccessSemaphore.Wait();
        System.IO.File.Delete(filename);
      }
      finally
      {
        _fileAccessSemaphore.Release();
      }
    }

    return Task.CompletedTask;
  }

  public Task Index(ContentMetaData metadata)
  {
    Abstractions.Models.Sitemap.Sitemap siteMap = this.SiteMap(metadata.Site);
    metadata.Site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PAGES_USE_SSL, out Boolean useSsl);
    SiteMapEntry item = siteMap.Items.Where(item => item.Url == metadata.Site.AbsoluteUrl(metadata.Url, useSsl)).FirstOrDefault();
    
    if (item == null)
    {
      item = new();
      siteMap.Items.Add(item);
    }

    if (metadata.Roles.Any() && !metadata.Roles.Contains(metadata.Site.AllUsersRole))
    {
      // item is not public, so it should not be included in the site map
      SiteMapEntry itemToRemove = siteMap.Items
        .Where(item => item.Url == metadata.Site.AbsoluteUrl(metadata.Url, useSsl))
        .FirstOrDefault();

      if (itemToRemove != null)
      {
        siteMap.Items.Remove(itemToRemove);
        this.Write(metadata.Site, siteMap);
      }
    }
    else
    {
      item.Url = metadata.Site.AbsoluteUrl(metadata.Url, useSsl);
      this.Write(metadata.Site, siteMap);
    }

    return Task.CompletedTask;
  }

  public Task Remove(ContentMetaData metadata)
  {
    Abstractions.Models.Sitemap.Sitemap siteMap = this.SiteMap(metadata.Site);
    metadata.Site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PAGES_USE_SSL, out Boolean useSsl);

    SiteMapEntry item = siteMap.Items
      .Where(item => item.Url == metadata.Site.AbsoluteUrl(metadata.Url, useSsl))
      .FirstOrDefault();

    if (item != null)
    {
      siteMap.Items.Remove(item);
    }

    Write(metadata.Site, siteMap);

    return Task.CompletedTask;
  }

  private static string GenerateValidPath(string value)
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

    try
    {
      _fileAccessSemaphore.Wait();
      using (System.IO.Stream output = System.IO.File.Open(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read))
      {
        System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Sitemap.Sitemap));
        serializer.Serialize(output, siteMap);
      }
    }
    finally
    {
      _fileAccessSemaphore.Release();
    }
  }
}
