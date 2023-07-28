using Nucleus.Abstractions.Managers;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Extensions;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.IO;
using Nucleus.Abstractions.Portable;
using Nucleus.DNN.Migration.ViewModels;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class FilesMigration : MigrationEngineBase<Models.DNN.Folder>
{
  private IFileSystemManager FileSystemManager { get; }

  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }
  private FileSystemProviderFactoryOptions FileSystemProviderOptions { get; }
  private IRoleManager RoleManager { get; }

  private HttpClient HttpClient { get; } = new();
  private Models.DNN.PortalAlias PortalAlias { get; set; }
  private Boolean UseSSL { get; set; }

  public FilesMigration(Nucleus.Abstractions.Models.Context context, IOptions<FileSystemProviderFactoryOptions> fileSystemProviderOptions, DNNMigrationManager migrationManager, IFileSystemManager fileSystemManager, IRoleManager roleManager) : base("Migrating Folders and Files")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.FileSystemManager = fileSystemManager;
    this.FileSystemProviderOptions = fileSystemProviderOptions.Value;
    this.RoleManager = roleManager;
  }

  public void SetAlias(Boolean useSSL, Models.DNN.PortalAlias portalAlias)
  {
    this.UseSSL = useSSL;
    this.PortalAlias = portalAlias;
  }

  public override async Task Migrate(Boolean updateExisting)
  {
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();
    List<Nucleus.Abstractions.Models.PermissionType> folderPermissionTypes = await this.FileSystemManager.ListFolderPermissionTypes();

    foreach (Models.DNN.Folder dnnFolder in this.Items)
    {
      if (dnnFolder.CanSelect && dnnFolder.IsSelected)
      {
        try
        {
          int successCount = 0;
          int failCount = 0;

          await ReadFolderProperties(dnnFolder);

          // DNN folder paths are in the form: /folder/subfolder/subsubfolder/
          string[] pathParts = dnnFolder.FolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
          string folderPath = pathParts.LastOrDefault() ?? "";
          string parentPath = string.Join('/', pathParts.Take(pathParts.Length - 1));
          string fullPath = string.Join('/', pathParts);

          Nucleus.Abstractions.Models.FileSystem.Folder nucleusFolder;

          try
          {
            nucleusFolder = await this.FileSystemManager.GetFolder(this.Context.Site, fileSystemProvider.Key, fullPath);
          }
          catch (FileNotFoundException)
          {
            nucleusFolder = null;
          }

          Boolean doCopyFiles = nucleusFolder == null || updateExisting;

          if (nucleusFolder == null)
          {
            // create folder            
            nucleusFolder = await this.FileSystemManager.CreateFolder(this.Context.Site, fileSystemProvider.Key, parentPath, folderPath);
          }

          if (nucleusFolder != null)
          {
            // migrate folder permissions
            await SetFolderPermissions(this.Context.Site, dnnFolder, nucleusFolder, folderPermissionTypes);
            await this.FileSystemManager.SaveFolderPermissions(this.Context.Site, nucleusFolder);
          }

          if (doCopyFiles)
          {
            foreach (Models.DNN.File dnnFile in dnnFolder.Files)
            {
              // copy files 

              // validate file
              AllowedFileType fileType = this.FileSystemProviderOptions.AllowedFileTypes
                .Where(allowedtype => allowedtype.FileExtensions.Contains(System.IO.Path.GetExtension(dnnFile.FileName), StringComparer.OrdinalIgnoreCase))
                .FirstOrDefault();

              if (fileType != null)
              {
                try
                {
                  Nucleus.Abstractions.Models.FileSystem.File existingNucleusFile;
                  try
                  {
                    existingNucleusFile = await this.FileSystemManager.GetFile(this.Context.Site, fileSystemProvider.Key, $"{dnnFile.Folder.FolderPath}{dnnFile.FileName}");
                  }
                  catch (System.IO.FileNotFoundException)
                  {
                    existingNucleusFile = null;
                  }

                  if (existingNucleusFile == null || updateExisting)
                  {
                    using (System.IO.Stream stream = await Download(dnnFile))
                    {
                      if (stream.Length == 0)
                      {
                        dnnFolder.AddWarning($"File '{dnnFile.FileName}' is zero length and was skipped.");
                        failCount++;
                      }
                      else if (!fileType.IsValid(stream))
                      {
                        string sample = BitConverter.ToString(Nucleus.Extensions.AllowedFileTypeExtensions.GetSample(stream)).Replace("-", "");
                        dnnFolder.AddWarning($"File content of file '{dnnFile.FileName}': signature [{sample}] does not match any of the file signatures for file type {System.IO.Path.GetExtension(dnnFile.FileName)}.");
                        failCount++;
                      }
                      else
                      {
                        // save the file
                        await this.FileSystemManager.SaveFile(this.Context.Site, fileSystemProvider.Key, dnnFile.Folder.FolderPath, dnnFile.FileName, stream, updateExisting);
                        successCount++;
                      }
                    }
                  }
                }
                catch (HttpRequestException ex)
                {
                  dnnFolder.AddWarning($"Error downloading file '{dnnFile.FileName}': {ex.Message}");
                  failCount++;
                }

              }
              else
              {
                dnnFolder.AddWarning($"Unsupported file type.  File '{dnnFile.FileName}' does not match any of the allowed file extensions.");
                failCount++;
              }
            }
          }
          
          dnnFolder.AddWarning($"{(successCount == 0 ? "No" : successCount)} file{(successCount == 1 ? "" : "s")} migrated, {(failCount==0 ? "no" : failCount)} file{(failCount == 1 ? "" : "s")} skipped or failed.");
        }
        catch (Exception ex)
        {
          dnnFolder.AddError($"Error importing folder '{dnnFolder.FolderPath}': {ex.Message}");
        }
        this.Progress();
      }
      else
      {
        dnnFolder.AddWarning($"Folder '{dnnFolder.FolderPath}' was not selected for import.");
      }
    }
  }


  private async Task ReadFolderProperties(Models.DNN.Folder folder)
  {
    Models.DNN.Folder dnnFolder = await this.MigrationManager.GetDnnFolder(folder.FolderId);

    folder.FolderPath = dnnFolder.FolderPath;
    folder.PortalId = dnnFolder.PortalId;

    folder.Files = dnnFolder.Files;
    folder.Permissions = dnnFolder.Permissions;
  }

  async Task<Nucleus.Abstractions.Models.Role> GetRole(Site site, FolderPermission dnnPermission)
  {
    if (dnnPermission.RoleName == "All Users" && dnnPermission.RoleId == -1)
    {
      // "all users" is represented in DNN by a RoleId of -1
      return site.AllUsersRole;
    }
    else if (dnnPermission.RoleName == "Unauthenticated Users" && dnnPermission.RoleId == -3)
    {
      // "anonymous users" is represented in DNN by a RoleId of -3
      return site.AnonymousUsersRole;
    }
    else
    {
      return await this.RoleManager.GetByName(this.Context.Site, dnnPermission.RoleName);
    }
  }

  async Task SetFolderPermissions(Site site, Models.DNN.Folder dnnFolder, Nucleus.Abstractions.Models.FileSystem.Folder newFolder, List<Nucleus.Abstractions.Models.PermissionType> folderPermissionTypes)
  {
    foreach (FolderPermission dnnPermission in dnnFolder.Permissions
            .Where(dnnPermission => dnnPermission.AllowAccess))
    {

      Nucleus.Abstractions.Models.Permission newPermission = new()
      {
        AllowAccess = true,
        PermissionType = GetFolderPermissionType(folderPermissionTypes, dnnPermission.PermissionKey),
        Role = await GetRole(site, dnnPermission)
      };

      if (newPermission.Role == null)
      {
        dnnFolder.AddWarning($"Folder permission '{dnnPermission.PermissionName}' for role '{dnnPermission.RoleName}' was not added because the role does not exist in Nucleus");
      }
      else if (newPermission.PermissionType == null)
      {
        if (dnnPermission.PermissionKey != "BROWSE")
        {
          dnnFolder.AddWarning($"Folder permission '{dnnPermission.PermissionName}' for role '{dnnPermission.RoleName}' was not added because the DNN permission key '{dnnPermission.PermissionKey}' was not expected");
        }
      }
      else if (newPermission.Role.Equals(this.Context.Site.AdministratorsRole))
      {
        // this doesn't need a warning
        //dnnFolder.AddWarning($"Folder permission '{dnnPermission.PermissionName}' for role '{dnnPermission.Role.RoleName}' was not added because Nucleus does not require role database entries for admin users");
      }
      else
      {
        Permission existing = newFolder.Permissions
          .Where(perm => perm.PermissionType.Scope == newPermission.PermissionType.Scope && perm.Role.Id == newPermission.Role.Id)
          .FirstOrDefault();

        if (existing == null)
        {
          newFolder.Permissions.Add(newPermission);
        }
        else
        {
          existing.AllowAccess = newPermission.AllowAccess;
        }
      }

    }
  }

  private PermissionType GetFolderPermissionType(List<Nucleus.Abstractions.Models.PermissionType> permissionTypes, string key)
  {
    switch (key)
    {
      case "READ":
        return permissionTypes.Where(permissionType => permissionType.Scope.Equals(PermissionType.PermissionScopes.FOLDER_VIEW)).FirstOrDefault();
      case "WRITE":
        return permissionTypes.Where(permissionType => permissionType.Scope.Equals(PermissionType.PermissionScopes.FOLDER_EDIT)).FirstOrDefault();
      case "BROWSE":
        // not supported by Nucleus
        break;
    }
    return null;
  }

  public override Task Validate()
  {
    foreach (Models.DNN.Folder folder in this.Items)
    {
      foreach (Models.DNN.File file in folder.Files)
      {
        AllowedFileType fileType = this.FileSystemProviderOptions.AllowedFileTypes.Where(allowedtype => allowedtype.FileExtensions.Contains(System.IO.Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase)).FirstOrDefault();
        if (fileType == null)
        {
          folder.AddWarning($"Unsupported file type.  File '{file.FileName}' does not match any of the allowed file extensions and will be skipped.");
        }
      }
    }

    return Task.CompletedTask;
  }

  private async Task<System.IO.Stream> Download(Models.DNN.File file)
  {
    string url = $"{(this.UseSSL ? "https" : "http")}://{this.PortalAlias.HttpAlias}/portals/{this.PortalAlias.PortalId}/{file.Folder.FolderPath}{file.FileName}";
    this.Message = $"Downloading {url} ...";

    HttpResponseMessage response = await this.HttpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStreamAsync();
  }
}
