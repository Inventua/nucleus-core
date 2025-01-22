using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Search;

public class FileMetaDataProducer : IContentMetaDataProducer
{
  private ISearchIndexHistoryManager SearchIndexHistoryManager { get; }

  private Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ExtensionProvider { get; } = new();

  private IFileSystemManager FileSystemManager { get; }

  private ILogger<FileMetaDataProducer> Logger { get; }

  public FileMetaDataProducer(ISearchIndexHistoryManager searchIndexHistoryManager, IFileSystemManager fileSystemManager, ILogger<FileMetaDataProducer> logger)
  {
    this.SearchIndexHistoryManager = searchIndexHistoryManager;
    this.FileSystemManager = fileSystemManager;
    this.Logger = logger;
  }

  public async override IAsyncEnumerable<ContentMetaData> ListItems(Site site)
  {
    if (!site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PUBLIC_FILES_ONLY, out bool indexPublicFilesOnly))
    {
      indexPublicFilesOnly = false;
    }

    if (site.DefaultSiteAlias == null)
    {
      this.Logger.LogWarning("Site {siteid} skipped because it does not have a default alias.", site.Id);
    }
    else
    {
      foreach (Abstractions.FileSystemProviders.FileSystemProviderInfo provider in this.FileSystemManager.ListProviders())
      {
        Folder rootFolder = await this.FileSystemManager.GetFolder(site, provider.Key, "");
        await foreach (ContentMetaData item in GetItems(site, rootFolder, indexPublicFilesOnly))
        {
          yield return item;
        }
      }
    }
  }

  private async IAsyncEnumerable<ContentMetaData> GetItems(Site site, Folder parentFolder, Boolean indexPublicFilesOnly)
  {
    Folder folder = await this.FileSystemManager.ListFolder(site, parentFolder.Id, "");

    if (!parentFolder.IncludeInSearch)
    {
      Logger.LogInformation("Skipping folder {folderid}[{provider}/{path}] because its 'Include in search' setting is false.", folder.Id, folder.Provider, folder.Path);
    }
    else
    {
      if (!indexPublicFilesOnly || folder.Permissions.Where(permission => permission.IsFolderViewPermission() && permission.AllowAccess).Any())
      {
        foreach (File file in folder.Files)
        {
          SearchIndexHistory historyItem = await this.SearchIndexHistoryManager.Get(site.Id, File.URN, file.Id);
          if (historyItem == null || historyItem.LastIndexedDate < file.DateModified)
          {
            Logger.LogTrace("Building meta-data for file {fileid}[{provider}/{path}]", file.Id, file.Provider, file.Path);
            ContentMetaData fileMetaData = await BuildContentMetaData(site, file);

            if (fileMetaData != null)
            {
              yield return fileMetaData;
            }
          }
          else
          {
            Logger.LogTrace("Skipped {fileid}[{provider}/{path}] because the file has not been changed since the last time it was indexed.", file.Id, file.Provider, file.Path);
          }
        }

        // Folders are indexed, but are not generally included in search results.  The file system manager can search for folders.
        foreach (Folder subfolder in folder.Folders)
        {
          SearchIndexHistory historyItem = await this.SearchIndexHistoryManager.Get(site.Id, Folder.URN, subfolder.Id);

          // the DateModified for a folder will generally not change, but we check it anyway.  The history check generally won't index a folder 
          // if it has been indexed before.
          if (historyItem == null || historyItem.LastIndexedDate < subfolder.DateModified)
          {
            Logger.LogTrace("Building meta-data for folder {fileid}[{provider}/{path}]", subfolder.Id, subfolder.Provider, subfolder.Path);
            ContentMetaData fileMetaData = await BuildContentMetaData(site, subfolder);

            if (fileMetaData != null)
            {
              yield return fileMetaData;
            }
          }
          else
          {
            Logger.LogTrace("Skipped {fileid}[{provider}/{path}] because the folder has already been indexed.", subfolder.Id, subfolder.Provider, subfolder.Path);
          }
        }
      }
      else
      {
        Logger.LogInformation("Skipping folder {folderid}[{provider}/{path}] because it is not visible to 'All users' and 'Index Public Files Only' is set.", folder.Id, folder.Provider, folder.Path);
      }

      foreach (Folder subFolder in folder.Folders)
      {
        await foreach (ContentMetaData item in GetItems(site, subFolder, indexPublicFilesOnly))
        {
          yield return item;
        }
      }
    }
  }

  private async Task<ContentMetaData> BuildContentMetaData(Site site, File file)
  {
    file = await this.FileSystemManager.GetFile(site, file.Id);

    string fileRelativeUrl = RelativeFileLink(file);

    if (!String.IsNullOrEmpty(fileRelativeUrl))
    {
      ContentMetaData contentItem = new()
      {
        Site = site,
        Title = file.Name,
        Url = fileRelativeUrl,
        RawUri = file.RawUri,
        PublishedDate = file.DateModified,
        Size = file.Size,
        SourceId = file.Id,
        Scope = File.URN,
        Type = "File",
        Roles = await GetViewRoles(file.Parent)
      };

      using (System.IO.Stream responseStream = await this.FileSystemManager.GetFileContents(site, file))
      {
        contentItem.Content = new byte[responseStream.Length];
        await responseStream.ReadExactlyAsync(contentItem.Content.AsMemory(0, contentItem.Content.Length));
        responseStream.Close();
      }

      if (!this.ExtensionProvider.TryGetContentType(file.Path, out string mimeType))
      {
        mimeType = "application/octet-stream";
      }

      contentItem.ContentType = mimeType;

      return contentItem;
    }

    Logger.LogWarning("Could not build page meta-data.");
    return null;
  }

  private async Task<ContentMetaData> BuildContentMetaData(Site site, Folder folder)
  {
    folder = await this.FileSystemManager.GetFolder(site, folder.Id);

    ContentMetaData contentItem = new()
    {
      Site = site,
      Title = folder.Name,
      Url = $"folder://{folder.Provider}/{folder.Path}",  // folders do not have a URL that can be used by end users
      SourceId = folder.Id,
      Scope = Folder.URN,
      Type = "Folder",
      Roles = await GetViewRoles(folder)
    };

    return contentItem;
  }

  private async Task<List<Role>> GetViewRoles(Folder folder)
  {
    return
      (await this.FileSystemManager.ListPermissions(folder))
        .Where(permission => permission.AllowAccess && permission.IsFolderViewPermission())
        .Select(permission => permission.Role).ToList();
  }

  private static string RelativeFileLink(File file)
  {
    string encodedPath = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{file.Id}"));
    return $"~/{Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH_PREFIX}/{encodedPath}";
  }
}
