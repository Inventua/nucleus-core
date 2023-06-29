using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class PageMigration : MigrationEngineBase<Models.DNN.Page>
{
  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }
  private IPageManager PageManager { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IRoleManager RoleManager { get; }

  public PageMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IRoleManager roleManager) : base("Pages")
  {
    this.Context = context;
    this.MigrationManager = migrationManager;
    this.PageManager = pageManager;
    this.PageModuleManager = pageModuleManager;
    this.RoleManager = roleManager;
  }

  public async override Task Migrate()
  {
    List<Nucleus.Abstractions.Models.PermissionType> pagePermissionTypes = await this.PageManager.ListPagePermissionTypes();
    List<Nucleus.Abstractions.Models.PermissionType> modulePermissionTypes = await this.PageModuleManager.ListModulePermissionTypes();
    IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> moduleDefinitions = await this.PageModuleManager.ListModuleDefinitions();

    Dictionary<int, Guid> CreatedPagesKeys = new();

    foreach (Models.DNN.Page dnnPage in this.Items)
    {
      if (dnnPage.CanSelect && dnnPage.IsSelected)
      {
        try
        {
          Nucleus.Abstractions.Models.Page newPage = await this.PageManager.CreateNew(this.Context.Site);

          newPage.Name = dnnPage.PageName;
          newPage.Title = dnnPage.Title;
          newPage.Description = dnnPage.Description;

          newPage.DisableInMenu = dnnPage.DisableLink;
          newPage.IncludeInSearch = true;
          newPage.Keywords = dnnPage.Keywords;
          newPage.ShowInMenu = dnnPage.IsVisible;
          newPage.SortOrder = dnnPage.TabOrder;

          AddRoutes(dnnPage, newPage);
          //newPage.DefaultPageRouteId doesn't need to be set, it defaults to the first route

          // TODO
          //newPage.DefaultContainerDefinition =?;
          //newPage.LayoutDefinition =?;

          if (dnnPage.ParentId != null)
          {
            if (CreatedPagesKeys.ContainsKey(dnnPage.ParentId.Value))
            {
              newPage.ParentId = CreatedPagesKeys[dnnPage.ParentId.Value];
            }
            else
            {
              dnnPage.AddWarning($"The Page parent could not be set because a page with DNN Parent ID '{dnnPage.ParentId}' was not imported.");
            }
          }

          await SetPagePermissions(dnnPage, newPage, pagePermissionTypes);

          //newPage.SiteId // this should already be set

          await AddPageModules(dnnPage, newPage, moduleDefinitions, modulePermissionTypes);
          
          await this.PageManager.Save(this.Context.Site, newPage);

          // page modules are saved separately
          foreach (Nucleus.Abstractions.Models.PageModule module in newPage.Modules)
          {
            await this.PageModuleManager.Save(newPage, module);
          }

          CreatedPagesKeys.Add(dnnPage.PageId, newPage.Id);
        }
        catch (Exception ex)
        {
          dnnPage.AddError($"Error importing role group '{dnnPage.PageName}': {ex.Message}");
        }

        this.Progress();
      }
      else
      {
        dnnPage.AddWarning($"Page '{dnnPage.PageName}' was not selected for import.");
      }
    }
  }

  void AddRoutes(Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage)
  {
    // remove the default/empty route that is auto-created
    newPage.Routes.Clear();

    // Add default route based on page hierachy
    newPage.Routes.Add(new()
    {
      Type = Abstractions.Models.PageRoute.PageRouteTypes.Active,
      Path = dnnPage.TabPath.Replace("//", "/")
    });

    // Add standard DNN "friendly" url format as a redirect to help maintain backward compatibility
    newPage.Routes.Add(new()
    {
      Type = Abstractions.Models.PageRoute.PageRouteTypes.PermanentRedirect,
      Path = $"{dnnPage.TabPath.Replace("//", "/")}/tabid/{dnnPage.PageId}/Default.aspx"
    });
  }

  async Task SetPagePermissions(Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage, List<Nucleus.Abstractions.Models.PermissionType> pagePermissionTypes)
  {
    foreach (PagePermission dnnPermission in dnnPage.Permissions
            .Where(dnnPermission => dnnPermission.AllowAccess && dnnPermission.Role != null))
    {
      Nucleus.Abstractions.Models.Permission newPermission = new()
      {
        AllowAccess = true,
        PermissionType = GetPermissionType(pagePermissionTypes, dnnPermission.PermissionKey),
        Role = await this.RoleManager.GetByName(this.Context.Site, dnnPermission.Role.RoleName)
      };

      if (newPermission.Role == null)
      {
        dnnPage.AddWarning($"Page permission '{dnnPermission.PermissionName}' for role '{dnnPermission.Role.RoleName}' was not added because the DNN permission key '{dnnPermission.PermissionKey}' was not expected");
      }
      else if (newPermission.PermissionType == null)
      {
        dnnPage.AddWarning($"Page permission '{dnnPermission.PermissionName}' for role '{dnnPermission.Role.RoleName}' was not added because the role does not exist in Nucleus");
      }
      else
      {
        newPage.Permissions.Add(newPermission);
      }
    }
  }

  async Task AddPageModules(Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage, IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> moduleDefinitions, List<Nucleus.Abstractions.Models.PermissionType> modulePermissionTypes)
  {
    foreach (Models.DNN.PageModule dnnModule in dnnPage.PageModules)
    {
      if (dnnModule.AllTabs)
      {
        dnnPage.AddWarning($"Page module '{dnnModule.ModuleTitle}' [{dnnModule.DesktopModule.ModuleName}] was not added because the DNN 'AllTabs' property is set, and Nucleus does not support this feature.");
      }
      else if (dnnModule.IsDeleted)
      {
        dnnPage.AddWarning($"Page module '{dnnModule.ModuleTitle}' [{dnnModule.DesktopModule.ModuleName}] was not added because it is marked as deleted.");
      }
      else
      {
        Nucleus.Abstractions.Models.PageModule newModule = await this.PageModuleManager.CreateNew(this.Context.Site);

        newModule.Title = dnnModule.ModuleTitle;
        newModule.InheritPagePermissions = dnnModule.InheritViewPermissions;
        newModule.Pane = dnnModule.PaneName;
        //newModule.ContainerDefinition=?;
        newModule.ModuleDefinition = MatchModuleDefinition(moduleDefinitions, dnnModule.DesktopModule);
        // newModule.ModuleSettings=?;
        //newModule.Permissions=?;
        newModule.SortOrder = dnnModule.ModuleOrder;


        foreach (PageModulePermission dnnModulePermission in dnnModule.Permissions
          .Where(dnnPermission => dnnPermission.AllowAccess && dnnPermission.Role != null))
        {
          Nucleus.Abstractions.Models.Permission newPermission = new()
          {
            AllowAccess = true,
            PermissionType = GetPermissionType(modulePermissionTypes, dnnModulePermission.PermissionKey),
            Role = await this.RoleManager.GetByName(this.Context.Site, dnnModulePermission.Role.RoleName)
          };

          if (newPermission.Role == null)
          {
            dnnPage.AddWarning($"Module permission '{dnnModulePermission.PermissionName}' for role '{dnnModulePermission.Role.RoleName}' was not added because the DNN permission key '{dnnModulePermission.PermissionKey}' was not expected");
          }
          else if (newPermission.PermissionType == null)
          {
            dnnPage.AddWarning($"Module permission '{dnnModulePermission.PermissionName}' for role '{dnnModulePermission.Role.RoleName}' was not added because the role does not exist in Nucleus");
          }
          else
          {
            newPage.Permissions.Add(newPermission);
          }
        }

        if (newModule.ModuleDefinition == null)
        {
          dnnPage.AddWarning($"Page module '{dnnModule.ModuleTitle}' [{dnnModule.DesktopModule.ModuleName}] was not added because the module type is not supported by this tool.");
        }
        else
        {
          newPage.Modules.Add(newModule);
        }
      }
    }
  }

  Nucleus.Abstractions.Models.ModuleDefinition MatchModuleDefinition(IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> modules, Models.DNN.DesktopModule desktopModule)
  {
    Guid? moduleDefinitionId = null;

    switch (desktopModule.ModuleName.ToLower())
    {
      case "dnn_html":
        moduleDefinitionId = new("b516d8dd-c793-4776-be33-902eb704bef6");
        break;

      case "authentication":
        moduleDefinitionId = new("f0a9ec71-c29e-436e-96e1-72dcdc44c32b");
        break;

      // these do no have "core" DNN equivalent modules
      //case "usersignup":
      //  moduleDefinitionId = new("7B25BDAF-14A3-4BAD-9C41-972DBBB384A1");
      //  break;


      //case "changepass":
      //  moduleDefinitionId = new("530EFACF-B9FF-4BF1-94D9-C357FC8769ED");
      //  break;

      case "viewprofile":
        moduleDefinitionId = new("1f347233-99e1-47b8-aa78-90ec16c6dbd2");
        break;

      case "console":
      case "sitemap":
        moduleDefinitionId = new("0392bf73-c646-4ccc-bcb5-372a75b9ea84");
        break;

      case "links":
      case "dnn_links":
        moduleDefinitionId = new("374e62b5-024d-4d8d-95a2-e56f476fe887");
        break;

      case "documents":
      case "dnn_documents":
        moduleDefinitionId = new("28df7ff3-6407-459e-8608-c1ef4181807c");
        break;

      case "media":
      case "dnn_media":
        moduleDefinitionId = new("2ffdf8a4-edab-48e5-80c6-7b068e4721bb");
        break;

        // , search input, search results, 
    }

    if (moduleDefinitionId != null)
    {
      return modules
          .Where(module => module.Id == moduleDefinitionId)
          .FirstOrDefault();
    }

    // no match
    return null;
  }

  private PermissionType GetPermissionType(List<Nucleus.Abstractions.Models.PermissionType> permissionTypes, string key)
  {
    switch (key)
    {
      case "VIEW":
        return permissionTypes.Where(permissionType => permissionType.Scope.Equals(PermissionType.PermissionScopes.PAGE_VIEW)).FirstOrDefault();
      case "EDIT":
        return permissionTypes.Where(permissionType => permissionType.Scope.Equals(PermissionType.PermissionScopes.PAGE_EDIT)).FirstOrDefault();
    }
    return null;
  }

  public override Task Validate()
  {
    foreach (Models.DNN.Page dnnPage in Items)
    {
      if (!String.IsNullOrEmpty(dnnPage.Url))
      {
        dnnPage.AddWarning("This page has a 'link type' set to redirect to another page or Url.  Nucleus does not support this feature.  You can migrate this page as an empty page and add a SiteMap module to it later.");
      }
    }


    return Task.CompletedTask;
  }
}
