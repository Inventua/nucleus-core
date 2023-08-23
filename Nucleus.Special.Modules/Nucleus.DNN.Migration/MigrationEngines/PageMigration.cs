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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Routing;

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
  private IFileSystemManager FileSystemManager { get; }
  private ILogger<PageMigration> Logger { get; }

  public List<Models.DNN.Skin> DNNSkins { get; set; }
  
  public List<Models.DNN.Container> DNNContainers { get; set; }

  private IEnumerable<Nucleus.DNN.Migration.MigrationEngines.ModuleContent.ModuleContentMigrationBase> ModuleContentMigrations { get; }

  public PageMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, IRoleManager roleManager, ILayoutManager layoutManager, IContainerManager containerManager, IEnumerable<Nucleus.DNN.Migration.MigrationEngines.ModuleContent.ModuleContentMigrationBase> moduleContentMigrations, ILogger<PageMigration> logger) : base("Migrating Pages and Modules")
  {
    this.Context = context;
    this.MigrationManager = migrationManager;
    this.PageManager = pageManager;
    this.PageModuleManager = pageModuleManager;
    this.FileSystemManager = fileSystemManager;
    this.LayoutManager = layoutManager;
    this.ContainerManager = containerManager;
    this.RoleManager = roleManager;
    this.ModuleContentMigrations = moduleContentMigrations;
    this.Logger = logger;
  }

  public void Setup(List<Models.DNN.Skin> skins, List<Models.DNN.Container> containers)
  {
    this.DNNSkins = skins;
    this.DNNContainers = containers;
  }

  public async override Task Migrate(Boolean updateExisting)
  {
    // we migrate pages and modules/content in two passes, so total count needs to be doubled
    this.TotalCount = this.TotalCount * 2;

    List<Nucleus.Abstractions.Models.PermissionType> pagePermissionTypes = await this.PageManager.ListPagePermissionTypes();
    List<Nucleus.Abstractions.Models.PermissionType> modulePermissionTypes = await this.PageModuleManager.ListModulePermissionTypes();
    IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> moduleDefinitions = await this.PageModuleManager.ListModuleDefinitions();
    Nucleus.Abstractions.FileSystemProviders.FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

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

          await AddRoutes(dnnPage, newPage);

          if (dnnPage.ContainerSrc != null)
          {
            Guid? assignedContainerId = this.DNNContainers
            .Where(container => container.ContainerSrc.Equals(dnnPage.ContainerSrc, StringComparison.OrdinalIgnoreCase))
            .Select(container => container.AssignedContainerId)
            .FirstOrDefault();

            if (assignedContainerId.HasValue)
            {
              newPage.DefaultContainerDefinition = (await this.ContainerManager.List())
                .Where(container => container.Id == assignedContainerId.Value)
                .FirstOrDefault();
            }
          }

          if (dnnPage.SkinSrc != null)
          {
            Guid? assignedLayoutId = this.DNNSkins
            .Where(skin => skin.SkinSrc.Equals(dnnPage.SkinSrc, StringComparison.OrdinalIgnoreCase))
            .Select(skin => skin.AssignedLayoutId)
            .FirstOrDefault();

            newPage.LayoutDefinition = (await this.LayoutManager.List())
              .Where(skin => skin.Id == assignedLayoutId.Value)
                .FirstOrDefault();
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
        dnnPage.AddInformation($"Page '{dnnPage.PageName}' was not selected for import.");
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
            await MigratePageModules(updateExisting, dnnPage, newPage, moduleDefinitions, modulePermissionTypes, createdPagesKeys);

            // handle DNN page link types
            if (!String.IsNullOrEmpty(dnnPage.Url))
            {
              // links to site pages are an integer (tabid).  Links to files are FileID=nn, and external links  (urls) are just the Url
              if (int.TryParse(dnnPage.Url, out int dnnPageLinkId))
              {
                // page link
                Models.DNN.Page dnnLinkPage = await this.MigrationManager.GetDnnPage(dnnPageLinkId);
                if (dnnLinkPage == null)
                {
                  dnnPage.AddWarning($"Unable to set page link for '{dnnPage.PageName}' because the linked DNN page id '{dnnPageLinkId}' was found.");
                }
                else
                {
                  Nucleus.Abstractions.Models.Page linkedPage = await this.PageManager.Get(this.Context.Site, dnnLinkPage.TabPath.Replace("//", "/"));
                  if (linkedPage == null)
                  {
                    dnnPage.AddWarning($"Unable to set page link for '{dnnPage.PageName}' because no Nucleus page with the path '{dnnLinkPage.TabPath.Replace("//", "/")}' was found.");
                  }
                  else
                  {
                    newPage.LinkType = Abstractions.Models.Page.LinkTypes.Page;
                    newPage.LinkPageId = linkedPage.Id;
                  }
                }
              }
              else if (dnnPage.Url.StartsWith("FileId=", StringComparison.OrdinalIgnoreCase))
              {
                if (dnnPage.Url.Length > "FileId=".Length && int.TryParse(dnnPage.Url.Substring("FileId=".Length), out int dnnFileLinkId))
                {
                  // file link
                  Models.DNN.File dnnLinkFile = await this.MigrationManager.GetDnnFile(dnnFileLinkId);
                  if (dnnLinkFile == null)
                  {
                    dnnPage.AddWarning($"Unable to set file link for '{dnnPage.PageName}' because the linked DNN page id '{dnnFileLinkId}' was found.");
                  }
                  else
                  {
                    Nucleus.Abstractions.Models.FileSystem.File linkedFile = await this.FileSystemManager.GetFile(this.Context.Site, fileSystemProvider.Key, dnnLinkFile.Path());
                    if (linkedFile == null)
                    {
                      dnnPage.AddWarning($"Unable to set file link for '{dnnPage.PageName}' because no Nucleus file with the path '{dnnLinkFile.Path()}' was found.");
                    }
                    else
                    {
                      newPage.LinkType = Abstractions.Models.Page.LinkTypes.File;
                      newPage.LinkFileId = linkedFile.Id;
                    }
                  }
                }
                else
                {
                  dnnPage.AddWarning($"Unable to set page link for '{dnnPage.PageName}' because the DNN file link value '{dnnPage.Url}' could not be parsed.");               
                }
              }
              else
              {
                // url link
                newPage.LinkType = Abstractions.Models.Page.LinkTypes.Url;
                newPage.LinkUrl = dnnPage.Url;
              }
            }
            
            // set parent page
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

            // ensure that page parent is not "itself".  
            if (newPage.ParentId == newPage.Id)
            {
              newPage.ParentId = null;
            }

            // save again for parent id + in case any module migrations added/ changed anything
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

    this.SignalCompleted();
  }

  private async Task AddRoutes(Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage)
  {
    List<PageUrl> urls;

    try
    {
      urls = await this.MigrationManager.ListDnnPageUrls(dnnPage.PageId);
    }
    catch (Exception ex)
    {
      urls = new();
    }

    if (urls.Any())
    {
      // if the DNN version supports TabUrls, use them
      foreach (PageUrl url in urls)
      {
        await AddRoute(dnnPage, newPage, url.HttpStatus == "301" ? PageRoute.PageRouteTypes.PermanentRedirect : PageRoute.PageRouteTypes.Active, url.Url + (String.IsNullOrEmpty(url.QueryString) ? "" : $"?{url.QueryString}"));
      }
    }
    else
    {
      // if the DNN version doesn't support TabUrls, create some generic routes

      // Add old-style DNN "search engine friendly" url format as a redirect to help maintain backward compatibility
      await AddRoute(dnnPage, newPage, Abstractions.Models.PageRoute.PageRouteTypes.PermanentRedirect, $"{dnnPage.TabPath.Replace("//", "/")}/tabid/{dnnPage.PageId}/Default.aspx");

      // Add old format "tabid=nn" url format as a redirect to help maintain backward compatibility
      await AddRoute(dnnPage, newPage, Abstractions.Models.PageRoute.PageRouteTypes.PermanentRedirect, $"/Default.aspx?tabid={dnnPage.PageId}");
    }

    // Add default route based on page hierarchy.  We always create this route because we use it to match to existing pages during migration.  We 
    // set this route as active only if there are no active routes.
    await AddRoute
    (
      dnnPage,
      newPage, 
      newPage.Routes.Where(route => route.Type == PageRoute.PageRouteTypes.Active).Any() ? PageRoute.PageRouteTypes.PermanentRedirect : PageRoute.PageRouteTypes.Active, 
      dnnPage.TabPath.Replace("//", "/")
    );
  }

  private async Task AddRoute(Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage, Abstractions.Models.PageRoute.PageRouteTypes type, string routePath)
  {
    Nucleus.Abstractions.Models.Page existing = await this.PageManager.Get(this.Context.Site, routePath);

    if (existing != null && existing.Id != newPage.Id)
    {
      dnnPage.AddWarning($"Route '{routePath}' was not added to the page '{newPage.Name}' because that route is already assigned to the '{existing.Name}' page.");
    }
    else
    {
      string path = routePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
      if (!String.IsNullOrEmpty(path))
      {
        if (Nucleus.Abstractions.RoutingConstants.RESERVED_ROUTES.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
          dnnPage.AddWarning($"Route '{routePath}' was not added to the page '{newPage.Name}' because page routes starting with '{path}' are reserved.");
          return;
        }
      }

      if (!newPage.Routes.Where(route => route.Path.Equals(routePath, StringComparison.OrdinalIgnoreCase)).Any())
      {
        newPage.Routes.Add(new()
        {
          Type = type,
          Path = routePath
        });
      }
    }
  }

  async Task<Nucleus.Abstractions.Models.Role> GetRole(Site site, PagePermission dnnPermission)
  {
    if (dnnPermission.RoleId.HasValue)
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
    else
    {
      return null;
    }
  }

  async Task SetPagePermissions(Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage, List<Nucleus.Abstractions.Models.PermissionType> pagePermissionTypes)
  {
    foreach (PagePermission dnnPermission in dnnPage.Permissions
            .Where(dnnPermission => dnnPermission.AllowAccess))
    {
      if (!dnnPermission.RoleId.HasValue)
      {
        dnnPage.AddWarning($"A per-user page permission '{dnnPermission.PermissionName}' for user '{dnnPermission.UserName}' was not added because Nucleus does not support per-user page permissions.");
      }
      else
      {
        Nucleus.Abstractions.Models.Permission newPermission = new()
        {
          AllowAccess = true,
          PermissionType = GetPagePermissionType(pagePermissionTypes, dnnPermission.PermissionKey),
          Role = await GetRole(this.Context.Site, dnnPermission)
        };

        if (newPermission.Role == null)
        {
          dnnPage.AddWarning($"Page permission '{dnnPermission.PermissionName}' for role '{dnnPermission.RoleName}' was not added because the role does not exist in Nucleus");
        }
        else if (newPermission.PermissionType == null)
        {
          dnnPage.AddWarning($"Page permission '{dnnPermission.PermissionName}' for role '{dnnPermission.RoleName}' was not added because the DNN permission key '{dnnPermission.PermissionKey}' was not expected");
        }
        else if (newPermission.Role.Equals(this.Context.Site.AdministratorsRole))
        {
          // this doesn't need a warning
          //dnnPage.AddWarning($"Page permission '{dnnPermission.PermissionName}' for role '{dnnPermission.RoleName}' was not added because Nucleus does not require role database entries for admin users");
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
  }

  async Task<Nucleus.Abstractions.Models.Role> GetRole(Site site, PageModulePermission dnnPermission)
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


  async Task SetPageModulePermissions(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Nucleus.Abstractions.Models.Page newPage, Nucleus.Abstractions.Models.PageModule newModule, List<Nucleus.Abstractions.Models.PermissionType> modulePermissionTypes)
  {
    foreach (PageModulePermission dnnModulePermission in dnnModule.Permissions
      .Where(dnnPermission => dnnPermission.AllowAccess))
    {
      if (!dnnModulePermission.RoleId.HasValue)
      {
        dnnPage.AddWarning($"A per-user module permission '{dnnModulePermission.PermissionName}' for user '{dnnModulePermission.UserName}' was not added because Nucleus does not support per-user module permissions.");
      }
      else
      {
        Nucleus.Abstractions.Models.Permission newPermission = new()
        {
          AllowAccess = true,
          PermissionType = GetPageModulePermissionType(modulePermissionTypes, dnnModulePermission.PermissionKey),
          Role = await GetRole(this.Context.Site, dnnModulePermission)
        };

        if (newPermission.Role == null)
        {
          dnnPage.AddWarning($"Module permission '{dnnModulePermission.PermissionName}' for role '{dnnModulePermission.RoleName}' was not added because the role does not exist in Nucleus");
        }
        else if (newPermission.PermissionType == null)
        {
          dnnPage.AddWarning($"Module permission '{dnnModulePermission.PermissionName}' for role '{dnnModulePermission.RoleName}' was not added because the DNN permission key '{dnnModulePermission.PermissionKey}' was not expected");
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
  }

  async Task MigratePageModules(Boolean updateExisting, Models.DNN.Page dnnPage, Nucleus.Abstractions.Models.Page newPage, IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> moduleDefinitions, List<Nucleus.Abstractions.Models.PermissionType> modulePermissionTypes, Dictionary<int, Guid> createdPagesKeys)
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
                    existing.Title?.Equals(dnnModule.ModuleTitle) == true
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

            if (!await ValidatePane(newPage.LayoutDefinition ?? this.Context.Site.DefaultLayoutDefinition, newModule.Pane))
            {
              dnnPage.AddWarning($"Page module '{dnnModule.ModuleTitle}' is assigned to pane '{newModule.Pane}' but that pane doesn't exist in the layout '{(newPage.LayoutDefinition ?? this.Context.Site.DefaultLayoutDefinition)?.FriendlyName}'.  You must edit the page and assign the module to a different pane, or use a different layout.");
            }

            Guid? containerId = this.DNNContainers
            .Where(container => container.ContainerSrc.Equals(dnnPage.ContainerSrc, StringComparison.OrdinalIgnoreCase))
            .Select(container => container.AssignedContainerId)
            .FirstOrDefault();

            if (containerId.HasValue)
            {
              newModule.ContainerDefinition = (await this.ContainerManager.List())
                .Where(container => container.Id == containerId.Value)
                .FirstOrDefault();
            }

            ////newModule.ContainerDefinition = (await this.ContainerManager.List())
            ////  .Where(container => container.FriendlyName.Equals(System.IO.Path.GetFileNameWithoutExtension(dnnModule.ContainerSrc), StringComparison.OrdinalIgnoreCase))
            ////  .FirstOrDefault();

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

  private async Task <Boolean> ValidatePane(LayoutDefinition layout, string paneName)
  {
    IEnumerable<string> panes = await this.LayoutManager.ListLayoutPanes(layout);
    return panes.Contains (paneName, StringComparer.OrdinalIgnoreCase);
  }

  public override Task Validate()
  {
    foreach (Models.DNN.Page dnnPage in Items)
    {
      string[] DEFAULT_UNSELECTED_PAGES = { "Friends", "Messages", "My Profile", "Activity Feed", "User Profile" };
      if (DEFAULT_UNSELECTED_PAGES.Contains(dnnPage.PageName, StringComparer.OrdinalIgnoreCase))
      {
        dnnPage.IsSelected = false;
        dnnPage.AddWarning($"The '{dnnPage.PageName}' page has a page name which is typically a built-in system page in DNN.  It has been un-selected by default, but you can choose to migrate it.");
      }      
    }


    return Task.CompletedTask;
  }
}
