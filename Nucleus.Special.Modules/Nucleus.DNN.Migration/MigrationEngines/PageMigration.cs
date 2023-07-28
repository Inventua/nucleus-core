using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.MigrationEngines.ModuleContent;
using Nucleus.Extensions;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml.Drawing;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class PageMigration : MigrationEngineBase<Models.DNN.Page>
{
  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }
  private IPageManager PageManager { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IRoleManager RoleManager { get; }
  private IContainerManager ContainerManager { get; }
  private ILayoutManager LayoutManager { get; }

  private ILogger<PageMigration> Logger { get; }

  private IEnumerable<Nucleus.DNN.Migration.MigrationEngines.ModuleContent.ModuleContentMigrationBase> ModuleContentMigrations { get; }

  public PageMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IRoleManager roleManager, ILayoutManager layoutManager, IContainerManager containerManager, IEnumerable<Nucleus.DNN.Migration.MigrationEngines.ModuleContent.ModuleContentMigrationBase> moduleContentMigrations, ILogger<PageMigration> logger) : base("Migrating Pages, Modules, Settings and Content")
  {
    this.Context = context;
    this.MigrationManager = migrationManager;
    this.PageManager = pageManager;
    this.PageModuleManager = pageModuleManager;
    this.LayoutManager = layoutManager;
    this.ContainerManager = containerManager;
    this.RoleManager = roleManager;
    this.ModuleContentMigrations = moduleContentMigrations;
    this.Logger = logger;
  }

  public async override Task Migrate(Boolean updateExisting)
  {
    // we migrate pages and modules/content in two passes, so total count needs to be doubled
    this.TotalCount = this.TotalCount * 2;

    List<Nucleus.Abstractions.Models.PermissionType> pagePermissionTypes = await this.PageManager.ListPagePermissionTypes();
    List<Nucleus.Abstractions.Models.PermissionType> modulePermissionTypes = await this.PageModuleManager.ListModulePermissionTypes();
    IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> moduleDefinitions = await this.PageModuleManager.ListModuleDefinitions();

    Dictionary<int, Guid> createdPagesKeys = new();

    foreach (Models.DNN.Page dnnPage in this.Items)
    {
      if (dnnPage.CanSelect && dnnPage.IsSelected)
      {
        try
        {
          Nucleus.Abstractions.Models.Page newPage = null;

          if (updateExisting)
          {
            newPage = await this.PageManager.Get(this.Context.Site, dnnPage.TabPath.Replace("//", "/"));
          }

          if (newPage == null)
          {
            newPage = await this.PageManager.CreateNew(this.Context.Site);

            // remove the default/empty route that is auto-created
            newPage.Routes.Clear();
          }

          newPage.Name = dnnPage.PageName;
          newPage.Title = dnnPage.Title;
          newPage.Description = dnnPage.Description;

          newPage.DisableInMenu = dnnPage.DisableLink;
          newPage.IncludeInSearch = true;
          newPage.Keywords = dnnPage.Keywords;
          newPage.ShowInMenu = dnnPage.IsVisible;
          newPage.SortOrder = dnnPage.TabOrder;

          AddRoutes(dnnPage, newPage);

          newPage.DefaultContainerDefinition = (await this.ContainerManager.List())
            .Where(container => container.FriendlyName.Equals(System.IO.Path.GetFileNameWithoutExtension(dnnPage.ContainerSrc), StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

          newPage.LayoutDefinition = (await this.LayoutManager.List())
            .Where(layout => layout.FriendlyName.Equals(System.IO.Path.GetFileNameWithoutExtension(dnnPage.ContainerSrc), StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

          if (dnnPage.ParentId != null)
          {
            if (createdPagesKeys.ContainsKey(dnnPage.ParentId.Value))
            {
              newPage.ParentId = createdPagesKeys[dnnPage.ParentId.Value];
            }
            else
            {
              // try looking by route
              Nucleus.Abstractions.Models.Page nucleusParentPage = await this.PageManager.Get(this.Context.Site, dnnPage.TabPath.Replace("//", "/"));
              if (nucleusParentPage != null)
              {
                newPage.ParentId = nucleusParentPage.Id;
              }
              else
              {
                dnnPage.AddWarning($"The Page parent could not be set because a page with DNN Parent ID '{dnnPage.ParentId}' was not imported, and a Nucleus page with route '{dnnPage.TabPath.Replace("//", "/")} was not found.'.");
              }
            }
          }

          await SetPagePermissions(dnnPage, newPage, pagePermissionTypes);

          try
          {
            // initial save to make sure that the page has an Id
            await this.PageManager.Save(this.Context.Site, newPage);
            createdPagesKeys.Add(dnnPage.PageId, newPage.Id);
          }
          catch (Exception ex)
          {
            this.Logger.LogError(ex, "New page creation failed");
            dnnPage.AddError($"New page creation failed: {ex.Message}");
          }
        }
        catch (Exception ex)
        {
          this.Logger.LogError(ex, "Error importing page '{pageName}", dnnPage.PageName);
          dnnPage.AddError($"Error importing page '{dnnPage.PageName}': {ex.Message}");
        }

        this.Progress();
      }
      else
      {
        dnnPage.AddWarning($"Page '{dnnPage.PageName}' was not selected for import.");
      }
    }

    // migrate modules and content.  We do this after creating all of the pages, because some modules have settings
    // which refer to pages which must already exist
    foreach (Models.DNN.Page dnnPage in this.Items)
    {
      if (dnnPage.CanSelect && dnnPage.IsSelected)
      {
        try
        {
          Nucleus.Abstractions.Models.Page newPage = await this.PageManager.Get(this.Context.Site, dnnPage.TabPath.Replace("//", "/"));

          if (newPage == null)
          {
            dnnPage.AddError($"Error importing modules and content for '{dnnPage.PageName}' because the migrated page could not be found.");
          }
          else
          {
            await AddPageModules(updateExisting, dnnPage, newPage, moduleDefinitions, modulePermissionTypes, createdPagesKeys);

            // if the page has an "Url" setting to indicate that it is a redirect to another page, add a SiteMap module to make it into a "landing"
            // page.  Nucleus does not support page "link types"
            if (!String.IsNullOrEmpty(dnnPage.Url))
            {
              // links to site pages are an integer (tabid).  Links to files are FileID=nn, and external links  (urls) are just the Url, we don't
              // support either of those.
              if (int.TryParse(dnnPage.Url, out int _))
              {
                await AddLandingPageModule(newPage, moduleDefinitions, updateExisting);
              }
            }

            // save again in case any module migrations added/ changed anything
            await this.PageManager.Save(this.Context.Site, newPage);
          }
        }
        catch (Exception ex)
        {
          this.Logger.LogError(ex, "Error importing page '{pageName}'", dnnPage.PageName);
          dnnPage.AddError($"Error importing page '{dnnPage.PageName}': {ex.Message}");
        }

        this.Progress();
      }
    }
  }

  // Create a SiteMap module to make the page a "landing page"
  private async Task AddLandingPageModule(Nucleus.Abstractions.Models.Page newPage, IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> moduleDefinitions, Boolean updateExisting)
  {
    Guid siteMapModuleDefinitionId = new("0392bf73-c646-4ccc-bcb5-372a75b9ea84");
    string siteMapTitle = newPage.Name;
    Nucleus.Abstractions.Models.PageModule newModule = null;

    if (updateExisting)
    {
      newModule = newPage.Modules
        .Where(existing =>
          existing.ModuleDefinition.Id == siteMapModuleDefinitionId &&
          (
            (String.IsNullOrEmpty(existing.Title) && String.IsNullOrEmpty(siteMapTitle))
            ||
            existing.Title.Equals(siteMapTitle)
          ) &&
          existing.SortOrder == 10)
        .FirstOrDefault();
    }

    if (newModule == null)
    {
      newModule = await this.PageModuleManager.CreateNew(this.Context.Site);
    }

    newModule.Title = siteMapTitle;
    newModule.InheritPagePermissions = true;
    newModule.Pane = "ContentPane";

    newModule.SortOrder = 10;

    newModule.ModuleDefinition = moduleDefinitions
      .Where(moduleDefinition => moduleDefinition.Id == siteMapModuleDefinitionId)
      .FirstOrDefault();

    newModule.ModuleSettings.Set("sitemap:maxlevels", 1);
    newModule.ModuleSettings.Set("sitemap:root-page-type", "CurrentPage");
    newModule.ModuleSettings.Set("sitemap:show-description", true);
    newModule.ModuleSettings.Set("sitemap:direction", "Vertical");

    await this.PageModuleManager.Save(newPage, newModule);
  }

  void AddRoutes(Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage)
  {
    // Add default route based on page hierachy
    AddRoute(newPage, Abstractions.Models.PageRoute.PageRouteTypes.Active, dnnPage.TabPath.Replace("//", "/"));

    // Add standard DNN "friendly" url format as a redirect to help maintain backward compatibility
    AddRoute(newPage, Abstractions.Models.PageRoute.PageRouteTypes.PermanentRedirect, $"{dnnPage.TabPath.Replace("//", "/")}/tabid/{dnnPage.PageId}/Default.aspx");

    // Add old format "tabid=nn" url format as a redirect to help maintain backward compatibility
    AddRoute(newPage, Abstractions.Models.PageRoute.PageRouteTypes.PermanentRedirect, $"/Default.aspx?tabid={dnnPage.PageId}");
  }

  void AddRoute(Nucleus.Abstractions.Models.Page newPage, Abstractions.Models.PageRoute.PageRouteTypes type, string routePath)
  {
    if (!newPage.Routes.Where(route => route.Path.Equals(routePath, StringComparison.OrdinalIgnoreCase)).Any())
    {
      newPage.Routes.Add(new()
      {
        Type = type,
        Path = routePath
      });
    }
  }

  async Task SetPagePermissions(Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage, List<Nucleus.Abstractions.Models.PermissionType> pagePermissionTypes)
  {
    foreach (PagePermission dnnPermission in dnnPage.Permissions
            .Where(dnnPermission => dnnPermission.AllowAccess && dnnPermission.Role != null))
    {
      Nucleus.Abstractions.Models.Permission newPermission = new()
      {
        AllowAccess = true,
        PermissionType = GetPagePermissionType(pagePermissionTypes, dnnPermission.PermissionKey),
        Role = await this.RoleManager.GetByName(this.Context.Site, dnnPermission.Role.RoleName)
      };

      if (newPermission.Role == null)
      {
        dnnPage.AddWarning($"Page permission '{dnnPermission.PermissionName}' for role '{dnnPermission.Role.RoleName}' was not added because the role does not exist in Nucleus");
      }
      else if (newPermission.PermissionType == null)
      {
        dnnPage.AddWarning($"Page permission '{dnnPermission.PermissionName}' for role '{dnnPermission.Role.RoleName}' was not added because the DNN permission key '{dnnPermission.PermissionKey}' was not expected");
      }
      else if (newPermission.Role.Equals(this.Context.Site.AdministratorsRole))
      {
        // this doesn't need a warning
        //dnnPage.AddWarning($"Page permission '{dnnPermission.PermissionName}' for role '{dnnPermission.Role.RoleName}' was not added because Nucleus does not require role database entries for admin users");
      }
      else
      {
        Permission existing = newPage.Permissions
          .Where(perm => perm.PermissionType.Scope == newPermission.PermissionType.Scope && perm.Role.Id == newPermission.Role.Id)
          .FirstOrDefault();

        if (existing == null)
        {
          newPage.Permissions.Add(newPermission);
        }
        else
        {
          existing.AllowAccess = newPermission.AllowAccess;
        }
      }
    }
  }

  async Task SetPageModulePermissions(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Nucleus.Abstractions.Models.Page newPage, Nucleus.Abstractions.Models.PageModule newModule, List<Nucleus.Abstractions.Models.PermissionType> modulePermissionTypes)
  {
    foreach (PageModulePermission dnnModulePermission in dnnModule.Permissions
      .Where(dnnPermission => dnnPermission.AllowAccess && dnnPermission.Role != null))
    {
      Nucleus.Abstractions.Models.Permission newPermission = new()
      {
        AllowAccess = true,
        PermissionType = GetPageModulePermissionType(modulePermissionTypes, dnnModulePermission.PermissionKey),
        Role = await this.RoleManager.GetByName(this.Context.Site, dnnModulePermission.Role.RoleName)
      };

      if (newPermission.Role == null)
      {
        dnnPage.AddWarning($"Module permission '{dnnModulePermission.PermissionName}' for role '{dnnModulePermission.Role.RoleName}' was not added because the role does not exist in Nucleus");
      }
      else if (newPermission.PermissionType == null)
      {
        dnnPage.AddWarning($"Module permission '{dnnModulePermission.PermissionName}' for role '{dnnModulePermission.Role.RoleName}' was not added because the DNN permission key '{dnnModulePermission.PermissionKey}' was not expected");
      }
      else if (newPermission.Role.Equals(this.Context.Site.AdministratorsRole))
      {
        // this doesn't need a warning
        //dnnPage.AddWarning($"Module permission '{dnnModulePermission.PermissionName}' for role '{dnnModulePermission.Role.RoleName}' was not added because Nucleus does not require role database entries for admin users");
      }
      else
      {
        Permission existing = newModule.Permissions
          .Where(perm => perm.PermissionType.Scope == newPermission.PermissionType.Scope && perm.Role.Id == newPermission.Role.Id)
          .FirstOrDefault();

        if (existing == null)
        {
          newModule.Permissions.Add(newPermission);
        }
        else
        {
          existing.AllowAccess = newPermission.AllowAccess;
        }
      }
    }
  }

  async Task AddPageModules(Boolean updateExisting, Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage, IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> moduleDefinitions, List<Nucleus.Abstractions.Models.PermissionType> modulePermissionTypes, Dictionary<int, Guid> createdPagesKeys)
  {
    foreach (Models.DNN.PageModule dnnModule in dnnPage.PageModules)
    {
      if (dnnModule.IsDeleted)
      {
        // we don't need a warning for this
        //dnnPage.AddWarning($"Page module '{dnnModule.ModuleTitle}' [{dnnModule.DesktopModule.ModuleName}] was not added because it is marked as deleted.");
      }
      else
      {
        Nucleus.Abstractions.Models.PageModule newModule = null;

        ModuleContentMigrationBase moduleMigrationimplementation = FindContentMigrationImplementation(moduleDefinitions, dnnPage, dnnModule, newPage, newModule);

        if (moduleMigrationimplementation != null)
        {
          // Guid.Empty is a special value, which is interpreted as meaning that a content migration exists, but we should
          // not create a module.  It is used for DNN modules like Google Analytics modules, which dont need a module instance
          // in Nucleus, but do contain the site's analytics ID which we can migrate.
          if (moduleMigrationimplementation.ModuleDefinitionId == Guid.Empty)
          {
            try
            {
              await moduleMigrationimplementation.MigrateContent(dnnPage, dnnModule, newPage, newModule, createdPagesKeys);
            }
            catch (NotImplementedException)
            {
              // ignore
            }
            catch (Exception ex)
            {
              dnnPage.AddError($"Page module '{dnnModule.ModuleTitle}' [{dnnModule.DesktopModule.ModuleName}] is a module type that migrates settings only, and migrating content failed with error: {ex.Message}");
            }
          }
          else
          {
            if (updateExisting)
            {
              newModule = newPage.Modules
                .Where(existing =>
                  existing.ModuleDefinition.Id == moduleMigrationimplementation.ModuleDefinitionId &&
                  (
                    (String.IsNullOrEmpty(existing.Title) && String.IsNullOrEmpty(dnnModule.ModuleTitle))
                    ||
                    existing.Title.Equals(dnnModule.ModuleTitle)
                  ) &&
                  existing.SortOrder == dnnModule.ModuleOrder)
                .FirstOrDefault();
            }

            if (newModule == null)
            {
              newModule = await this.PageModuleManager.CreateNew(this.Context.Site);
            }

            newModule.Title = dnnModule.ModuleTitle;
            newModule.InheritPagePermissions = dnnModule.InheritViewPermissions;
            newModule.Pane = dnnModule.PaneName;

            newModule.ContainerDefinition = (await this.ContainerManager.List())
              .Where(container => container.FriendlyName.Equals(System.IO.Path.GetFileNameWithoutExtension(dnnModule.ContainerSrc), StringComparison.OrdinalIgnoreCase))
              .FirstOrDefault();

            newModule.SortOrder = dnnModule.ModuleOrder;

            await SetPageModulePermissions(dnnPage, dnnModule, newPage, newModule, modulePermissionTypes);

            newModule.ModuleDefinition = moduleDefinitions
              .Where(moduleDefinition => moduleDefinition.Id == moduleMigrationimplementation.ModuleDefinitionId)
              .FirstOrDefault();

            if (newModule.ModuleDefinition == null)
            {
              dnnPage.AddWarning($"Page module '{dnnModule.ModuleTitle}' [{dnnModule.DesktopModule.ModuleName}] was not added to the page because the target Nucleus extension '{moduleMigrationimplementation.ModuleFriendlyName}' is not installed.");
            }
            else
            {
              // Special case: validate that there is only one login module on a page
              if (newModule.ModuleDefinition.Id == Guid.Parse("f0a9ec71-c29e-436e-96e1-72dcdc44c32b") && 
                 (newPage.Modules
                    .Where(module => module.ModuleDefinition.Id == Guid.Parse("f0a9ec71-c29e-436e-96e1-72dcdc44c32b"))
                    .Count()) > 1)
              {                
                dnnPage.AddWarning($"A login module was not added to the '{dnnPage.PageName}' page because it already contains a login module.");
                return;
              }

              // we must save the page module record before adding content
              try
              {
                // initial save to make sure the module has an Id
                await this.PageModuleManager.Save(newPage, newModule);
                await this.PageModuleManager.SavePermissions(newModule);
              }
              catch (Exception ex)
              {
                dnnPage.AddError($"Page module '{dnnModule.ModuleTitle}' [{dnnModule.DesktopModule.ModuleName}] was not added to the page because there was an error: {ex.Message}");
                return;
              }

              // migrate module content            
              try
              {
                await moduleMigrationimplementation.MigrateContent(dnnPage, dnnModule, newPage, newModule, createdPagesKeys);

                // save again in case content migrations changed anything
                await this.PageModuleManager.Save(newPage, newModule);

              }
              catch (NotImplementedException)
              {
                // ignore
              }
              catch (Exception ex)
              {
                this.Logger.LogError(ex, "Page module '{moduleTitle}' [{moduleName}] was added to the page, but migrating content failed.", dnnModule.ModuleTitle, dnnModule.DesktopModule.ModuleName);
                dnnPage.AddError($"Page module '{dnnModule.ModuleTitle}' [{dnnModule.DesktopModule.ModuleName}] was added to the page, but migrating content failed with error: {ex.Message}");
              }
            }
          }
        }
      }
    }
  }

  private ModuleContentMigrationBase FindContentMigrationImplementation(IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> modules, Models.DNN.Page dnnPage, Models.DNN.PageModule dnnPageModule, Nucleus.Abstractions.Models.Page newPage, Nucleus.Abstractions.Models.PageModule newModule)
  {
    foreach (ModuleContentMigrationBase implementation in this.ModuleContentMigrations)
    {
      if (dnnPageModule.DesktopModule == null)
      {
        dnnPage.AddWarning($"Page module '{dnnPageModule.ModuleTitle}' was not added to the page because there was no module type (DesktopModule) data for it.");
      }
      else
      {
        if (implementation.IsMatch(dnnPageModule.DesktopModule))
        {
          return implementation;
        }
      }
    }

    dnnPage.AddWarning($"The module '{dnnPageModule.ModuleTitle}' was not added to the '{newPage.Name}' page because we don't have a module content migration implementation for the '{dnnPageModule.DesktopModule.ModuleName}' module.");

    // no match
    return null;
  }

  private PermissionType GetPagePermissionType(List<Nucleus.Abstractions.Models.PermissionType> permissionTypes, string key)
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

  private PermissionType GetPageModulePermissionType(List<Nucleus.Abstractions.Models.PermissionType> permissionTypes, string key)
  {
    switch (key)
    {
      case "VIEW":
        return permissionTypes.Where(permissionType => permissionType.Scope.Equals(PermissionType.PermissionScopes.MODULE_VIEW)).FirstOrDefault();
      case "EDIT":
        return permissionTypes.Where(permissionType => permissionType.Scope.Equals(PermissionType.PermissionScopes.MODULE_EDIT)).FirstOrDefault();
    }
    return null;
  }

  public override Task Validate()
  {
    foreach (Models.DNN.Page dnnPage in Items)
    {
      if (!String.IsNullOrEmpty(dnnPage.Url))
      {
        dnnPage.AddWarning("This page has a 'link type' set to redirect to another page or Url.  Nucleus does not support this feature.  This page will be migrated with a pre-configured SiteMap module so that it can act as a 'landing' page with a list of child pages.");
      }
    }


    return Task.CompletedTask;
  }
}
