using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Managers;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Nucleus.DNN.Migration.Models.DNN.Modules.NTForums;
using System.IO;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class NTForumsModuleContentMigration : ModuleContentMigrationBase
{
  private DNNMigrationManager DnnMigrationManager { get; }
  private ISiteManager SiteManager { get; }
  private IRoleManager RoleManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IPermissionsManager PermissionsManager { get; }

  private static class PermissionScopeNamespaces
  {
    public const string Forum = "urn:nucleus:entities:forum/permissiontype";
  }

  private static class PermissionScopes
  {
    public static string FORUM_VIEW = $"{PermissionScopeNamespaces.Forum}/{PermissionType.PermissionScopeTypes.VIEW}";
    public static string FORUM_EDIT_POST = $"{PermissionScopeNamespaces.Forum}/{PermissionType.PermissionScopeTypes.EDIT}";
    public const string FORUM_CREATE_POST = PermissionScopeNamespaces.Forum + "/createpost";
    public const string FORUM_REPLY_POST = PermissionScopeNamespaces.Forum + "/reply";

    public const string FORUM_DELETE_POST = PermissionScopeNamespaces.Forum + "/delete";
    public const string FORUM_LOCK_POST = PermissionScopeNamespaces.Forum + "/lock";
    public const string FORUM_ATTACH_POST = PermissionScopeNamespaces.Forum + "/attach";
    public const string FORUM_SUBSCRIBE = PermissionScopeNamespaces.Forum + "/subscribe";

    public const string FORUM_PIN_POST = PermissionScopeNamespaces.Forum + "/pin";
    public const string FORUM_MODERATE = PermissionScopeNamespaces.Forum + "/moderate";
    public const string FORUM_UNMODERATED = PermissionScopeNamespaces.Forum + "/unmoderated";
  }

  public NTForumsModuleContentMigration(DNNMigrationManager dnnMigrationManager, ISiteManager siteManager, IRoleManager roleManager, IFileSystemManager fileSystemManager, IPermissionsManager permissionManager)
  {
    this.DnnMigrationManager = dnnMigrationManager;
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
    this.RoleManager = roleManager;
    this.PermissionsManager = permissionManager;
  }

  public override string ModuleFriendlyName => "Forums";

  public override Guid ModuleDefinitionId => new("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "NTForums" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage.SiteId);
    Nucleus.Abstractions.Portable.IPortable portable = this.DnnMigrationManager.GetPortableImplementation(this.ModuleDefinitionId);
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();
    Nucleus.Abstractions.Models.FileSystem.Folder attachmentsFolder;
    
    try
    {
      attachmentsFolder = await this.FileSystemManager.GetFolder(site, fileSystemProvider.Key, "NTForums_Attach");
    }
    catch (FileNotFoundException)
    {
      attachmentsFolder = null;
    }

    foreach (ForumGroup dnnGroup in await this.DnnMigrationManager.ListDnnNTForumGroupsByModule(dnnModule.ModuleId))
    {
      List<object> forums = new();

      foreach (Forum dnnForum in dnnGroup.Forums)
      {

        object newForumSettings = new
        {
          Enabled = dnnForum.Active,
          Visible = !dnnForum.Hidden,
          IsModerated = dnnForum.IsModerated,
          AllowAttachments = !dnnForum.AttachCount.HasValue ? false : dnnForum.AttachCount.Value > 0,
          AllowSearchIndexing = dnnForum.IndexContent,
          AttachmentsFolder = attachmentsFolder
        };

        object newForum = new
        {
          _type = "Forum",
          Name = dnnForum.Name,
          Description = dnnForum.Description,
          SortOrder = dnnForum.SortOrder ?? 0,
          Settings = newForumSettings,
          UseGroupSettings = dnnForum.InheritGroupSettings,
          Permissions = await BuildForumPermissions(site, dnnPage, dnnForum)
        };

        forums.Add(newForum);
      }

      object newGroupSettings = new
      {
        Enabled = dnnGroup.Settings.Active,
        Visible = !dnnGroup.Settings.Hidden,
        IsModerated = dnnGroup.Settings.IsModerated,
        AllowAttachments = !dnnGroup.Settings.AttachCount.HasValue ? false : dnnGroup.Settings.AttachCount.Value > 0,
        AllowSearchIndexing = dnnGroup.Settings.IndexContent
      };

      object newGroup = new
      {
        _type = "ForumGroup",
        ModuleId = newModule.Id,
        Name = dnnGroup.Name,
        SortOrder = dnnGroup.SortOrder ?? 0,
        Settings = newGroupSettings,
        Forums = forums,
        Permissions = await BuildGroupPermissions(site, dnnPage, dnnGroup)
      };

      await portable.Import(newModule, new Nucleus.Abstractions.Portable.PortableContent("urn:nucleus:entities:forum-group", newGroup ));
    }
  }

  private async Task<List<Permission>> BuildGroupPermissions(Site site, Models.DNN.Page dnnPage, ForumGroup dnnGroup)
  {
    List<Permission> permissions = new();

    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.AttachToPostRoles, PermissionScopes.FORUM_ATTACH_POST, $"Forum Group '{dnnGroup.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.ViewRoles, PermissionScopes.FORUM_VIEW, $"Forum Group '{dnnGroup.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.ReplyPostRoles, PermissionScopes.FORUM_REPLY_POST, $"Forum Group '{dnnGroup.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.CreatePostRoles, PermissionScopes.FORUM_CREATE_POST, $"Forum Group '{dnnGroup.Name}'"));

    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.DeletePostRoles, PermissionScopes.FORUM_DELETE_POST, $"Forum Group '{dnnGroup.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.EditPostRoles, PermissionScopes.FORUM_EDIT_POST, $"Forum Group '{dnnGroup.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.LockPostRoles, PermissionScopes.FORUM_LOCK_POST, $"Forum Group '{dnnGroup.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.PinPostRoles, PermissionScopes.FORUM_PIN_POST, $"Forum Group '{dnnGroup.Name}'"));

    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.SubscribePostRoles, PermissionScopes.FORUM_SUBSCRIBE, $"Forum Group '{dnnGroup.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Settings.ModeratorRoles, PermissionScopes.FORUM_MODERATE, $"Forum Group '{dnnGroup.Name}'"));

    return permissions;
  }

  private async Task<List<Permission>> BuildForumPermissions(Site site, Models.DNN.Page dnnPage, Forum dnnForum)
  {
    List<Permission> permissions = new();

    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.AttachToPostRoles, PermissionScopes.FORUM_ATTACH_POST, $"Forum '{dnnForum.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.ViewRoles, PermissionScopes.FORUM_VIEW, $"Forum '{dnnForum.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.ReplyPostRoles, PermissionScopes.FORUM_REPLY_POST, $"Forum '{dnnForum.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.CreatePostRoles, PermissionScopes.FORUM_CREATE_POST, $"Forum '{dnnForum.Name}'"));

    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.DeletePostRoles, PermissionScopes.FORUM_DELETE_POST, $"Forum '{dnnForum.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.EditPostRoles, PermissionScopes.FORUM_EDIT_POST, $"Forum '{dnnForum.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.LockPostRoles, PermissionScopes.FORUM_LOCK_POST, $"Forum '{dnnForum.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.PinPostRoles, PermissionScopes.FORUM_PIN_POST, $"Forum '{dnnForum.Name}'"));

    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.SubscribePostRoles, PermissionScopes.FORUM_SUBSCRIBE, $"Forum '{dnnForum.Name}'"));
    permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.ModeratorRoles, PermissionScopes.FORUM_MODERATE, $"Forum '{dnnForum.Name}'"));

    return permissions;
  }

  private async Task<List<Permission>> BuildPermissions(Site site, Models.DNN.Page dnnPage, string roles, string scope, string displayName)
  {
    List<Permission> permissions = new();

    foreach (string roleId in roles.Split(';', StringSplitOptions.RemoveEmptyEntries))
    {
      if (int.TryParse(roleId, out int dnnRoleId))
      {
        // ignore negative values (which seem to mean "deny")
        if (dnnRoleId >= 0)
        {
          Models.DNN.Role dnnRole = await this.DnnMigrationManager.GetDnnRole(dnnRoleId);
          if (dnnRole != null && dnnRole.RoleName != "Administrators")
          {
            Nucleus.Abstractions.Models.Role nucleusRole = await this.RoleManager.GetByName(site, dnnRole.RoleName);

            if (nucleusRole != null)
            {
              PermissionType permissionType = (await this.PermissionsManager.ListPermissionTypes(PermissionScopeNamespaces.Forum))
                  .Where(permissionType => permissionType.Scope == scope)
                  .FirstOrDefault();

              if (permissionType != null)
              {
                permissions.Add(new()
                {
                  PermissionType = permissionType,
                  Role = nucleusRole,
                  AllowAccess = true
                });
              }
              else
              {
                dnnPage.AddWarning($"Permission '{scope}'/'{nucleusRole.Name}' for {displayName} was not set because a permission with scope '{scope}' was not found.");
              }
            }
            else
            {
              dnnPage.AddWarning($"Permission '{scope}'/'{dnnRole.RoleName}' for {displayName} was not set because a role with name '{dnnRole.RoleName}' was not found.");
            }
          }
        }
      }
    }

    return permissions;
  }

}
