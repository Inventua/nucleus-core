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

namespace Nucleus.Core.Search
{
	public class FileMetaDataProducer : IContentMetaDataProducer
	{
		private Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ExtensionProvider  { get; } = new();

		private IFileSystemManager FileSystemManager { get; }
		
		private ILogger<FileMetaDataProducer> Logger { get; }

		public FileMetaDataProducer(ISiteManager siteManager, IFileSystemManager fileSystemManager, ILogger<FileMetaDataProducer> logger)
		{
			this.FileSystemManager = fileSystemManager;
			this.Logger = logger;			
		}

		public async override Task<IEnumerable<ContentMetaData>> ListItems(Site site)
		{
			List<ContentMetaData> results = new();
			Boolean indexPublicFilesOnly = false;

			site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PUBLIC_FILES_ONLY, out indexPublicFilesOnly);

			if (site.DefaultSiteAlias == null)
			{
				this.Logger.LogWarning("Site {0} skipped because it does not have a default alias.", site.Id);
			}
			else
			{
				foreach (Abstractions.FileSystemProviders.FileSystemProviderInfo provider in this.FileSystemManager.ListProviders())
				{
					Folder rootFolder = await this.FileSystemManager.GetFolder(site, provider.Key, "");
					results.AddRange(await GetFiles(site, rootFolder, indexPublicFilesOnly));
				}
			}

			return results;
		}		

		private async Task<List<ContentMetaData>> GetFiles(Site site, Folder parentFolder, Boolean indexPublicFilesOnly)
		{
			List<ContentMetaData> results = new();

			Folder folder = await this.FileSystemManager.ListFolder(site, parentFolder.Id, "");

			if (!indexPublicFilesOnly || folder.Permissions.Where(permission => permission.IsFolderViewPermission() && permission.AllowAccess).Any())
			{
				foreach (File file in folder.Files)
				{
					Logger.LogInformation("Building meta-data for file {0}[{1}/{2}]", file.Id, file.Provider, file.Path);
					ContentMetaData metaData = await BuildContentMetaData(site, file);

					if (metaData != null)
					{
						results.Add(metaData);					
					}
				}

				foreach (Folder subFolder in folder.Folders)
				{
					results.AddRange(await GetFiles(site, subFolder, indexPublicFilesOnly));				
				}
			}
			else
			{
				Logger.LogInformation("Skipping folder {0}[{1}/{2}] because it is not visible to 'All users' and 'Index Public Files Only' is set.", folder.Id, folder.Provider, folder.Path);
			}
			

			return results;
		}

		private async Task<ContentMetaData> BuildContentMetaData(Site site, File file)
		{
			file = await this .FileSystemManager.GetFile(site, file.Id);

			string fileRelativeUrl = RelativeFileLink(file);

			if (!String.IsNullOrEmpty(fileRelativeUrl))
			{				
				ContentMetaData contentItem = new()
				{
					Site = site,
					Title = file.Name,
					Url = fileRelativeUrl,
					PublishedDate = file.DateModified,
					Size = file.Size,
					SourceId = file.Id,
					Scope = File.URN,
					Roles = await GetViewRoles(file.Parent)
				};
												
				using (System.IO.Stream responseStream = this.FileSystemManager.GetFileContents(site, file))
				{
					contentItem.Content = new byte[responseStream.Length];
					await responseStream.ReadAsync(contentItem.Content.AsMemory(0, contentItem.Content.Length));
					responseStream.Close();
				}

				string mimeType = "application/octet-stream";
				this.ExtensionProvider.TryGetContentType(file.Path, out mimeType);

				contentItem.ContentType = mimeType;

				return contentItem;
			}

			Logger.LogWarning("Could not build page meta-data.");
			return null;
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
			return $"~/files/{encodedPath}";			
		}
	}
}
