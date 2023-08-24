using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class ActiveForumsModuleContentMigration : ModuleContentMigrationBase
{
  private DNNMigrationManager DnnMigrationManager { get; }
  private ISiteManager SiteManager { get; }
  private IRoleManager RoleManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IPermissionsManager PermissionsManager { get; }

  private const string FORUM_SETTING_ALLOWATTACH  = "ALLOWATTACH";
  private const string FORUM_SETTING_ALLOWEMOTICONS = "ALLOWEMOTICONS";
  private const string FORUM_SETTING_ALLOWHTML = "ALLOWHTML";
  private const string FORUM_SETTING_ALLOWPOSTICON = "ALLOWPOSTICON";
  private const string FORUM_SETTING_ALLOWRSS = "ALLOWRSS";
  private const string FORUM_SETTING_ALLOWSCRIPT = "ALLOWSCRIPT";
  private const string FORUM_SETTING_ALLOWSUBSCRIBE = "ALLOWSUBSCRIBE";
  private const string FORUM_SETTING_ATTACHCOUNT = "ATTACHCOUNT";
  private const string FORUM_SETTING_ATTACHMAXHEIGHT = "ATTACHMAXHEIGHT";
  private const string FORUM_SETTING_ATTACHMAXSIZE = "ATTACHMAXSIZE";
  private const string FORUM_SETTING_ATTACHMAXWIDTH = "ATTACHMAXWIDTH";
  private const string FORUM_SETTING_ATTACHSTORE = "ATTACHSTORE";
  private const string FORUM_SETTING_ATTACHTYPEALLOWED = "ATTACHTYPEALLOWED";
  private const string FORUM_SETTING_ATTACHUNIQUEFILENAMES = "ATTACHUNIQUEFILENAMES";
  private const string FORUM_SETTING_EDITORHEIGHT = "EDITORHEIGHT";
  private const string FORUM_SETTING_EDITORSTYLE = "EDITORSTYLE";
  private const string FORUM_SETTING_EDITORTOOLBAR = "EDITORTOOLBAR";
  private const string FORUM_SETTING_EDITORTYPE = "EDITORTYPE";
  private const string FORUM_SETTING_EDITORWIDTH = "EDITORWIDTH";
  private const string FORUM_SETTING_EMAILADDRESS = "EMAILADDRESS";
  private const string FORUM_SETTING_INDEXCONTENT = "INDEXCONTENT";
  private const string FORUM_SETTING_INSTALLDATE = "INSTALLDATE";
  private const string FORUM_SETTING_ISMODERATED = "ISMODERATED";
  private const string FORUM_SETTING_TOPICSTEMPLATEID = "TOPICSTEMPLATEID";
  private const string FORUM_SETTING_TOPICTEMPLATEID = "TOPICTEMPLATEID";
  private const string FORUM_SETTING_USEFILTER = "USEFILTER";

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

  public ActiveForumsModuleContentMigration(DNNMigrationManager dnnMigrationManager, ISiteManager siteManager, IRoleManager roleManager, IFileSystemManager fileSystemManager, IPermissionsManager permissionManager)
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
    string[] matches = { "Active Forums" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage.SiteId);
    Nucleus.Abstractions.Portable.IPortable portable = this.DnnMigrationManager.GetPortableImplementation(this.ModuleDefinitionId);
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    foreach (ForumGroup dnnGroup in await this.DnnMigrationManager.ListDnnActiveForumsGroupsByModule(dnnModule.ModuleId))
    {
      List<object> forums = new();

      foreach (Forum dnnForum in dnnGroup.Forums)
      {
        object newForumSettings = new
        {
          Enabled = dnnForum.Active,
          Visible = !dnnForum.Hidden,
          IsModerated = GetSetting(dnnForum.Settings, FORUM_SETTING_ISMODERATED, false),
          AllowAttachments = GetSetting(dnnForum.Settings, FORUM_SETTING_ALLOWATTACH, false),
          AllowSearchIndexing = GetSetting(dnnForum.Settings, FORUM_SETTING_INDEXCONTENT, false)
        };

        object newForum = new
        {
          _type = "Forum",
          Name = dnnForum.Name,
          Description = dnnForum.Description,
          SortOrder = dnnForum.SortOrder ?? 0,
          Settings = newForumSettings,
          UseGroupSettings = dnnForum.Permissions == null,
          Permissions = await BuildForumPermissions(site, dnnPage, dnnForum)
        };

        forums.Add(newForum);
      }

      object newGroupSettings = new
      {
        Enabled = dnnGroup.Active,
        Visible = !dnnGroup.Hidden,
        IsModerated = GetSetting(dnnGroup.Settings, FORUM_SETTING_ISMODERATED, false),
        AllowAttachments = GetSetting(dnnGroup.Settings, FORUM_SETTING_ALLOWATTACH, false),
        AllowSearchIndexing = GetSetting(dnnGroup.Settings, FORUM_SETTING_INDEXCONTENT, false)
      };

      object newGroup = new
      {
        _type = "ForumGroup",
        ModuleId = newModule.Id,
        Name = dnnGroup.Name,
        SortOrder = dnnGroup.SortOrder,
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

    if (dnnGroup.Permissions != null)
    {
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.AttachToPostRoles, PermissionScopes.FORUM_ATTACH_POST, $"Forum Group '{dnnGroup.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.ViewRoles, PermissionScopes.FORUM_VIEW, $"Forum Group '{dnnGroup.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.ReplyPostRoles, PermissionScopes.FORUM_REPLY_POST, $"Forum Group '{dnnGroup.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.CreatePostRoles, PermissionScopes.FORUM_CREATE_POST, $"Forum Group '{dnnGroup.Name}'"));

      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.DeletePostRoles, PermissionScopes.FORUM_DELETE_POST, $"Forum Group '{dnnGroup.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.EditPostRoles, PermissionScopes.FORUM_EDIT_POST, $"Forum Group '{dnnGroup.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.LockPostRoles, PermissionScopes.FORUM_LOCK_POST, $"Forum Group '{dnnGroup.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.PinPostRoles, PermissionScopes.FORUM_PIN_POST, $"Forum Group '{dnnGroup.Name}'"));

      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.SubscribePostRoles, PermissionScopes.FORUM_SUBSCRIBE, $"Forum Group '{dnnGroup.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnGroup.Permissions.ModeratorRoles, PermissionScopes.FORUM_MODERATE, $"Forum Group '{dnnGroup.Name}'"));
    }

    return permissions;
  }

  private async Task<List<Permission>> BuildForumPermissions(Site site, Models.DNN.Page dnnPage, Forum dnnForum)
  {
    List<Permission> permissions = new();

    if (dnnForum.Permissions != null)
    {
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.AttachToPostRoles, PermissionScopes.FORUM_ATTACH_POST, $"Forum '{dnnForum.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.ViewRoles, PermissionScopes.FORUM_VIEW, $"Forum '{dnnForum.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.ReplyPostRoles, PermissionScopes.FORUM_REPLY_POST, $"Forum '{dnnForum.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.CreatePostRoles, PermissionScopes.FORUM_CREATE_POST, $"Forum '{dnnForum.Name}'"));

      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.DeletePostRoles, PermissionScopes.FORUM_DELETE_POST, $"Forum '{dnnForum.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.EditPostRoles, PermissionScopes.FORUM_EDIT_POST, $"Forum '{dnnForum.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.LockPostRoles, PermissionScopes.FORUM_LOCK_POST, $"Forum '{dnnForum.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.PinPostRoles, PermissionScopes.FORUM_PIN_POST, $"Forum '{dnnForum.Name}'"));

      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.SubscribePostRoles, PermissionScopes.FORUM_SUBSCRIBE, $"Forum '{dnnForum.Name}'"));
      permissions.AddRange(await BuildPermissions(site, dnnPage, dnnForum.Permissions.ModeratorRoles, PermissionScopes.FORUM_MODERATE, $"Forum '{dnnForum.Name}'"));
    }

    return permissions;
  }

  private async Task<List<Permission>> BuildPermissions(Site site, Models.DNN.Page dnnPage, string permissionRoles, string scope, string displayName)
  {
    List<Permission> permissions = new();

    // ActiveForums stores permissions as role-id-list|user-id-list|social-group-id-list.  We are only interested in role-id-list
    // Reference: https://github.com/ActiveForums/ActiveForums/blob/master/class/Permissions.cs line 240
    string roles = permissionRoles.Split('|').FirstOrDefault();

    if (roles != null)
    {
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
    }

    return permissions;
  }

  private T GetSetting<T>(List<ForumSetting> settings, string key, T defaultValue = default(T))
  {
    ForumSetting setting = settings
      .Where(setting => setting.SettingName.Equals(key, StringComparison.OrdinalIgnoreCase))
      .FirstOrDefault();

    if (setting == null)
    {
      return defaultValue;
    }  
    else if (typeof(T) == typeof(Boolean))
    {
      if (Boolean.TryParse(setting.SettingValue, out Boolean boolResult))
      {
        return (T)Convert.ChangeType(boolResult, typeof(T));
      }
      else if (setting.SettingValue == "0")
      {
        return (T)Convert.ChangeType(false, typeof(T));
      }
      else if (setting.SettingValue == "1")
      {
        return (T)Convert.ChangeType(true, typeof(T));
      }
      else
      {
        return (T)Convert.ChangeType(setting.SettingValue, typeof(T));
      }
    }
    else
    {
      return (T)Convert.ChangeType(setting.SettingValue, typeof(T));
    }
  }
}
