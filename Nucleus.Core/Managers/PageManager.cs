using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Core;
using System.Security.Claims;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;
using System.Security.Cryptography;
using Nucleus.Extensions;
using System.Text.RegularExpressions;

namespace Nucleus.Core.Managers;

/// <summary>
/// Provides functions to manage database data for <see cref="Page"/>s.
/// </summary>
public partial class PageManager : IPageManager
{
  // "partial" class is required by GeneratedRegex

  [Flags]
  private enum PermissionsCheckOption
  {
    Ignore = 0,
    View = 1,
    Edit = 2,
    Both = View | Edit
  }

  [GeneratedRegexAttribute(@"({\$guid[0-9]+})", RegexOptions.ECMAScript)]
  private static partial System.Text.RegularExpressions.Regex GuidTokenRegEx();


  [GeneratedRegex(@"<(.+)>\s*<(.+)>([\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12})", RegexOptions.ECMAScript)]
  private static partial Regex FindGuidRegEx();

  private IDataProviderFactory DataProviderFactory { get; }
  private ICacheManager CacheManager { get; }
  private IPermissionsManager PermissionsManager { get; }
  public PageManager(IDataProviderFactory dataProviderFactory, IPermissionsManager permissionsManager, ICacheManager cacheManager)
  {
    this.CacheManager = cacheManager;
    this.DataProviderFactory = dataProviderFactory;
    this.PermissionsManager = permissionsManager;
  }

  /// <summary>
  /// Create a new <see cref="Page"/> with default values.
  /// </summary>
  /// <param name="site"></param>
  /// <returns></returns>
  /// <remarks>
  /// The new <see cref="Page"/> is not saved to the database until you call <see cref="Save(Site, Page)"/>.
  /// </remarks>
  public Task<Page> CreateNew(Site site)
  {
    Page result = new();

    // default route (url)
    result.Routes.Add(new PageRoute());

    return Task.FromResult(result);
  }

  /// <summary>
  /// Retrieve an existing <see cref="Page"/> from the database.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public async Task<Page> Get(Guid id)
  {
    if (id == Guid.Empty) return default;

    return await this.CacheManager.PageCache().GetAsync(id, async id =>
    {
      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
      {
        return await provider.GetPage(id);
      }
    });
  }

  /// <summary>
  /// Retrieve an existing page from the database, specified by site and path.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="path"></param>
  /// <returns></returns>
  public async Task<Page> Get(Site site, string path)
  {
    // ensure that path starts with '/' and does not end with '/', but if the path is equal to '/' leave as-is.  '/' is
    // normally the route to the site's home page.
    if (path != "/")
    {
      path = String.Concat(!path.StartsWith('/') ? "/" : "", path.EndsWith('/') ? path[0..^1] : path);
    }

    return await FindPage(site, path);
  }

  /// <summary>
  /// Return the page matching the specified path, utilizing the cache.  Return null if there is no match.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="path"></param>
  /// <returns></returns>
  private async Task<Page> FindPage(Site site, string path)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      Guid pageId = await provider.FindPage(site, path);

      if (pageId == Guid.Empty && String.IsNullOrEmpty(path))
      {
        // treat empty local path as "/"
        pageId = await provider.FindPage(site, "/");
      }

      if (pageId == Guid.Empty)
      {
        return null;
      }
      else
      {
        return await Get(pageId);
      }
    }
  }

  /// <summary>
  /// Retrive an existing page from the database.  The returned page is the "owner" of the specified module.
  /// </summary>
  /// <param name="page"></param>
  /// <returns></returns>
  public async Task<Page> Get(PageModule pageModule)
  {
    return await Get(pageModule.PageId);
  }

  /// <summary>
  /// List all <see cref="PageModule"/>s that are part of the page.
  /// </summary>
  /// <param name="page"></param>
  /// <returns></returns>
  public async Task<List<PageModule>> ListModules(Page page)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      return await provider.ListPageModules(page.Id);
    }
  }

  /// <summary>
  /// Delete the specifed <see cref="Page"/> from the database.
  /// </summary>
  /// <param name="page"></param>
  public async Task Delete(Page page)
  {
    // page objects from http requests/which have been created by MVC don't always have a valid site id, so we must get 
    // the site from the database.
    Page existing = await this.Get(page.Id);

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      List<Page> childPages = await provider.ListPages(existing.SiteId, page.Id);
      if (childPages.Any())
      {
        throw new InvalidOperationException($"This page has {childPages.Count} child page{(childPages.Count == 1 ? "" : "s")}.  You must delete the child page{(childPages.Count == 1 ? "" : "s")} before you can delete this page.");
      }
    }

    await this.PermissionsManager.DeletePermissions(page.Permissions);

    if (page.Modules != null)
    {
      foreach (PageModule module in page.Modules)
      {
        await this.PermissionsManager.DeletePermissions(module.Permissions);
        using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
        {
          await provider.DeletePageModule(module);
        }
      }
    }

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      await provider.DeletePage(page);
    }

    InvalidateCache(page.Id);
  }

  /// <summary>
  /// List all <see cref="Page"/>s within the specified site.
  /// </summary>
  /// <param name="site"></param>
  /// <returns></returns>
  /// <remarks>
  /// This function returns a fully populated list of pages and is intended for the use by Search Index providers and 
  /// other components where performance is not important. Use ListSitePages for a faster result where page objects are not
  /// fully populated.
  /// </remarks>
  public async Task<IList<Page>> List(Site site)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      return await provider.ListPages(site.Id);
    }
  }

  /// <summary>
  /// List all <see cref="Page"/>s within the specified site.
  /// </summary>
  /// <param name="site"></param>
  /// <returns></returns>
  /// <remarks>
  /// This function returns a simple/not fully populated list of pages and is intended for use when populating drop-down
  /// lists, etc.
  /// </remarks>
  public async Task<IList<Page>> ListSitePages(Site site)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      return await provider.ListSitePages(site.Id);
    }
  }

  /// <summary>
  /// Return a list of all <see cref="Page"/>s for the site which match the specified search term.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="searchTerm"></param>
  /// <param name="userRoleNames"></param>
  /// <param name="pagingSettings"></param>
  /// <returns></returns>
  public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<Page>> Search(Site site, string searchTerm, IEnumerable<Role> userRoles, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      return await provider.SearchPages(site, searchTerm, userRoles, pagingSettings);
    }
  }

  /// <summary>
  /// Add default permissions to the specifed <see cref="Page"/> for the specified <see cref="Role"/>.
  /// </summary>
  /// <param name="page"></param>
  /// <param name="role"></param>
  /// <remarks>
  /// The new permissions are not saved unless you call <see cref="Save(Site, Page)"/>.
  /// </remarks>
  public async Task CreatePermissions(Site site, Page page, Role role)
  {
    Boolean isAnonymousOrAllUsers = role.Equals(site.AnonymousUsersRole) || role.Equals(site.AllUsersRole);

    using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
    {
      {
        List<PermissionType> permissionTypes = await provider.ListPermissionTypes(Page.URN);
        List<Permission> permissions = [];

        foreach (PermissionType permissionType in permissionTypes)
        {
          Permission permission = new();
          permission.Role = role;

          if (isAnonymousOrAllUsers && !permissionType.IsPageViewPermission())
          {
            permission.AllowAccess = false;
            permission.PermissionType = new() { Scope = PermissionType.PermissionScopeNamespaces.Disabled };
          }
          else
          {
            permission.AllowAccess = permissionType.IsPageViewPermission();
            permission.PermissionType = permissionType;
          }

          permissions.Add(permission);
        }

        page.Permissions.AddRange(permissions);
      }
    }
  }

  /// <summary>
  /// List all permissions for the specified <see cref="Page"/>, sorted by <see cref="Role"/> name and <see cref="PermissionType"/> <see cref="PermissionType.SortOrder"/>.
  /// </summary>
  /// <param name="page"></param>
  /// <returns></returns>
  public async Task<List<Permission>> ListPermissions(Page page)
  {
    using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
    {
      List<PermissionType> permissionTypes = await provider.ListPermissionTypes(Page.URN);
      List<Permission> permissions = await provider.ListPermissions(page.Id, Page.URN);
      Dictionary<Role, IList<Permission>> results = [];

      // ensure that for each role with any permissions defined, there is a full set of permission types for the role
      foreach (Role role in permissions.Select((permission) => permission.Role).ToList())
      {
        foreach (PermissionType permissionType in permissionTypes)
        {
          if (permissions.Where((permission) => permission?.Role.Id == role.Id && permission?.PermissionType.Id == permissionType.Id).ToList().Count == 0)
          {
            Permission permission = new();
            permission.AllowAccess = false;
            permission.PermissionType = permissionType;
            permission.Role = role;
            permissions.Add(permission);
          }
        }
      }

      return permissions;
    }
  }

  /// <summary>
  /// Return a list of available permission types, sorted by SortOrder
  /// </summary>
  /// <returns></returns>
  public async Task<List<PermissionType>> ListPagePermissionTypes()
  {
    using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
    {
      return (await provider.ListPermissionTypes(Page.URN)).OrderBy(permissionType => permissionType.SortOrder).ToList();
    }
  }

  /// <summary>
  /// Create or update a <see cref="Page"/>, including its <see cref="Page.Permissions"/> and <see cref="Page.Routes"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  public async Task Save(Site site, Page page)
  {
    List<Permission> currentPermissions = await this.PermissionsManager.ListPermissions(page.Id, Page.URN);

    // check for reserved routes
    foreach (PageRoute route in page.Routes)
    {
      string path = route.Path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
      if (!String.IsNullOrEmpty(path))
      {
        if (RoutingConstants.RESERVED_ROUTES.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
          throw new InvalidOperationException($"Page routes starting with '{path}' are reserved.");
        }
      }
    }

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      await provider.SavePage(site, page);
    }

    await this.PermissionsManager.SavePermissions(page.Id, page.Permissions, currentPermissions);

    InvalidateCache(page.Id);
  }

  /// <summary>
  /// Export the specified <see cref="Page"/> as XML.
  /// </summary>
  /// <param name="page"></param>
  /// <remarks>
  /// Creates a page template which can be used to create new pages.
  /// </remarks>
  public async Task<System.IO.Stream> Export(Page page)
  {
    string xmlData;

    Nucleus.Abstractions.Models.Export.PageTemplate export = new()
    {
      Page = page.Copy<Page>()
    };

    // page template can't use the same page name and title
    export.Page.Name = "";
    export.Page.Title = "";

    // page templates can't have routes or permissions
    export.Page.Routes.Clear();

    export.Page.Permissions.Clear();
    
    foreach (PageModule module in export.Page.Modules)
    {
      module.Permissions.Clear();
    }

    using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
    {
      export.Contents = [];
      foreach (PageModule pageModule in export.Page.Modules)
      {
        export.Contents.AddRange(await provider.ListContent(pageModule));
      }
    }

    // serialize the export data to XML
    using (System.IO.MemoryStream serializedSite = new())
    {
      System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Export.PageTemplate));
      serializer.Serialize(serializedSite, export);

      // deserialize the xml to a string for additional processing
      xmlData = System.Text.Encoding.UTF8.GetString(serializedSite.ToArray());
    }

    // replace id (guid) values with tokens, except for id's which refer to module definitions, permission types, container definitions and layout definitions
    MatchCollection idMatches = FindGuidRegEx().Matches(xmlData);

    int tokenIndex = 1;
    foreach (System.Text.RegularExpressions.Match match in idMatches)
    {
      if (match.Groups.Count == 4)
      {
        string nodeName = match.Groups[1].Value;
        string propertyName = match.Groups[2].Value;
        string idValue = match.Groups[3].Value;

        switch (nodeName)
        {
          case nameof(ModuleDefinition):
          case nameof(PermissionType):
          case nameof(ContainerDefinition):
          case nameof(LayoutDefinition): 
            // skip these node types, as they are all Guids that must be the same across all sites
            break;

          default:
            // For other ids, replace with a token.  The replace operation must be for the whole file, as some id's may be present in 
            // more than one location.  This method isn't particularly fast, but it doesn't need to be since exporting a site happens 
            // infrequently.
            if (idValue != Guid.Empty.ToString())
            {
              xmlData = xmlData.Replace(idValue, $"{{$guid{tokenIndex}}}");
            }

            tokenIndex++;
            break;
        }
      }
    }

    // convert the string back to a stream and return
    return new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlData));
  }


  private void InvalidateCache(Guid pageId)
  {
    this.CacheManager.PageCache().Remove(pageId);
    this.CacheManager.PageMenuCache().Clear();
    this.CacheManager.PageRouteCache().Clear();
    this.CacheManager.AdminPageMenuCache().Clear();
  }

  /// <summary>
  /// Ensure that pages have unique sort order.
  /// </summary>
  /// <param name="pages"></param>
  /// <remarks>
  /// Page sort orders can produce duplicates and gaps when pages parents are changed, or pages are deleted.
  /// </remarks>
  private async Task CheckNumbering(Site site, List<Page> pages)
  {
    int sortOrder = 10;

    foreach (Page page in pages)
    {
      if (page.SortOrder != sortOrder)
      {
        page.SortOrder = sortOrder;
        //await provider.SavePage(site, page);
        await this.Save(site, page);
        this.CacheManager.PageCache().Remove(page.Id);
      }

      sortOrder += 10;
    }
  }

  /// <summary>
  /// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="Page.SortOrder"/>.
  /// </summary>
  /// <param name="pageId"></param>
  public async Task MoveDown(Site site, Guid pageId)
  {
    Page previousPage = null;
    Page thisPage;
    List<Page> siblingPages;

    thisPage = await this.Get(pageId);
      
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      siblingPages = await provider.ListPages(site.Id, thisPage.ParentId);
    }

    await CheckNumbering(site, siblingPages);

    siblingPages.Reverse();
    foreach (Page page in siblingPages)
    {
      if (page.Id == pageId)
      {
        if (previousPage != null)
        {
          int temp = page.SortOrder;
          page.SortOrder = previousPage.SortOrder;
          previousPage.SortOrder = temp;

          if (previousPage.SortOrder == page.SortOrder)
          {
            page.SortOrder += 10;
          }

          await this.Save(site, previousPage);
          await this.Save(site, page);

          InvalidateCache(page.Id);
          InvalidateCache(previousPage.Id);
          break;
        }
      }
      else
      {
        previousPage = page;
      }
    }
  }


  /// <summary>
  /// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the previous <see cref="Page.SortOrder"/>.
  /// </summary>
  /// <param name="pageId"></param>
  public async Task MoveUp(Site site, Guid pageId)
  {
    Page previousPage = null;
    Page thisPage;
    List<Page> siblingPages;

    thisPage = await this.Get(pageId);

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      siblingPages = await provider.ListPages(site.Id, thisPage.ParentId);
    }

    await CheckNumbering(site, siblingPages);

    foreach (Page page in siblingPages)
    {
      if (page.Id == pageId)
      {
        if (previousPage != null)
        {
          int temp = page.SortOrder;
          page.SortOrder = previousPage.SortOrder;
          previousPage.SortOrder = temp;

          if (previousPage.SortOrder == page.SortOrder)
          {
            previousPage.SortOrder += 10;
          }

          await this.Save(site, previousPage);
          await this.Save(site, page);

          InvalidateCache(page.Id);
          InvalidateCache(previousPage.Id);
          break;
        }
      }
      else
      {
        previousPage = page;
      }
    }
  }

  public async Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, int levels, Boolean ignorePermissions, Boolean ignoreDisabled, Boolean ignoreShowInMenu)
  {
    string key;
    if (user.IsSystemAdministrator())
    {
      key = $"system-administrator:{site.Id}:{levels}:{ignorePermissions}:{ignoreDisabled}:{ignoreShowInMenu}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
    }
    else if (user.IsSiteAdmin(site))
    {
      key = $"site-administrator:{site.Id}:{levels}:{ignorePermissions}:{ignoreDisabled}:{ignoreShowInMenu}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
    }
    else
    {
      string roles = String.Join(":", user.Claims.Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value));
      if (String.IsNullOrEmpty(roles))
      {
        roles = "Anonymous User";
      }
      key = String.Join("|", Encode(roles) + $":{site.Id}:{levels}:{ignorePermissions}:{ignoreDisabled}:{ignoreShowInMenu}:{(parentPage == null ? Guid.Empty : parentPage.Id)}");
    }

    return await this.CacheManager.AdminPageMenuCache().GetAsync(key, async key =>
    {
      // read from database
      PageMenuChildrenResult childrenResult =
      await GetPageMenuDescendants
      (
        site,
        parentPage,
        user,
        levels,
        0,
        ignorePermissions ? PermissionsCheckOption.Ignore : PermissionsCheckOption.View,
        ignoreDisabled,
        ignoreShowInMenu
      );

      return new PageMenu(null, childrenResult.Children, childrenResult.HasChildren);
    });
  }

  public async Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, int levels)
  {
    return await GetAdminMenu(site, parentPage, user, levels, true, true, true);
  }

  public async Task<PageMenu> GetAdminMenu(Site site, Page parentPage, ClaimsPrincipal user, Guid? selectedPageId)
  {

    string key;
    if (user.IsSystemAdministrator())
    {
      key = $"system-administrator:{site.Id}:{selectedPageId}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
    }
    else if (user.IsSiteAdmin(site))
    {
      key = $"site-administrator:{site.Id}:{selectedPageId}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
    }
    else
    {
      string roles = String.Join(":", user.Claims.Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value));
      if (String.IsNullOrEmpty(roles))
      {
        roles = "Anonymous User";
      }
      key = String.Join("|", Encode(roles) + $":{site.Id}:{selectedPageId}:{(parentPage == null ? Guid.Empty : parentPage.Id)}");
    }

    return await this.CacheManager.AdminPageMenuCache().GetAsync(key, async key =>
    {
      // read from database
      if (selectedPageId.HasValue && selectedPageId != Guid.Empty)
      {
        // use "breadcrumb" logic to fill in the tree up to the selected page
        List<Page> breadcrumbs = [];

        Page breadcrumbPage = await Get(selectedPageId.Value);
        do
        {
          if (breadcrumbPage != null)
          {
            breadcrumbs.Add(breadcrumbPage);

            // extra checks here are to prevent an infinite loop 
            if (breadcrumbPage.ParentId.HasValue && breadcrumbPage.ParentId != breadcrumbPage.Id && !breadcrumbs.Where(crumb => crumb.Id == breadcrumbPage.ParentId).Any())
            {
              breadcrumbPage = await this.Get(breadcrumbPage.ParentId.Value);
            }
            else
            {
              breadcrumbPage = null;
            }
          }

        } while (breadcrumbPage != null);

        if (breadcrumbs.Any())
        {
          breadcrumbs.Reverse();

          // get the top level
          PageMenu menu = await GetAdminMenu(site, null, user, 1, true, true, true);
          PageMenu subMenu = menu;

          foreach (Page ancestor in breadcrumbs)
          {
            subMenu = subMenu.Children.Where(child => child.Page.Id == ancestor.Id).FirstOrDefault();

            PageMenuChildrenResult newChildren = await GetPageMenuDescendants(site, subMenu.Page, user, 1, 0, PermissionsCheckOption.Ignore, true, true);
            subMenu.UpdateChildren(newChildren.Children, newChildren.HasChildren);
          }

          return menu;
        }
      }

      return await GetAdminMenu(site, parentPage, user, 1);
    });
  }


  public async Task<PageMenu> GetMenu(Site site, Page parentPage, ClaimsPrincipal user, Boolean ignoreSettings)
  {
    string key;
    if (user.IsSystemAdministrator())
    {
      key = $"system-administrator:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
    }
    else if (user.IsSiteAdmin(site))
    {
      key = $"site-administrator:{site.Id}:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}";
    }
    else
    {
      string roles = String.Join(":", user.Claims.Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value));
      if (String.IsNullOrEmpty(roles))
      {
        roles = "Anonymous User";
      }
      key = String.Join("|", Encode(roles) + $":{site.Id}:{ignoreSettings}:{(parentPage == null ? Guid.Empty : parentPage.Id)}");
    }

    return await this.CacheManager.PageMenuCache().GetAsync(key, async key =>
    {
      // read from database
      PageMenuChildrenResult childrenResult = await GetPageMenuDescendants(site, parentPage, user, 0, 0, PermissionsCheckOption.View, ignoreSettings, ignoreSettings);
      return new PageMenu(null, childrenResult.Children, childrenResult.HasChildren);
    });
  }

  /// <summary>
  /// Copy permissions from the specified <paramref name="page"/> to its descendants.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  /// <param name="operation">
  /// If <paramref name="operation"/> is <see cref="IPageManager.CopyPermissionOperation.Replace"/> overwrite all permissions of descendant pages.  
  /// if <paramref name="operation"/> is <see cref="IPageManager.CopyPermissionOperation.Merge"/>, merge the descendant pages permissions with the specified 
  /// <paramref name="page"/> permissions.		
  /// </param>
  /// <returns></returns>
  public async Task<Boolean> CopyPermissionsToDescendants(Site site, Page page, ClaimsPrincipal user, CopyPermissionOperation operation)
  {
    PageMenuChildrenResult children = await GetPageMenuDescendants(site, page, user, 0, 0, PermissionsCheckOption.Edit, true, true);

    List<Permission> permissions = await this.ListPermissions(page);

    if (children.HasChildren)
    {
      foreach (PageMenu child in children.Children)
      {
        await CopyPermissionsToDescendants(site, child, permissions, operation);
      }
      return true;
    }
    else
    {
      return false;
    }
  }

  /// <summary>
  /// Parse a template file and return the deserialized result
  /// </summary>
  /// <param name="stream"></param>
  /// <returns></returns>
  public Task<Nucleus.Abstractions.Models.Export.PageTemplate> ParseTemplate(System.IO.Stream stream)
  {
    // First, get the stream as a string
    System.IO.StreamReader reader = new(stream);
    string xmlData = reader.ReadToEnd();

    // Replace the GUID tokens with newly-generated guids
    MatchCollection idMatches = GuidTokenRegEx().Matches(xmlData);

    foreach (System.Text.RegularExpressions.Match match in idMatches)
    {
      if (match.Success && match.Groups.Count > 1)
      {
        xmlData = xmlData.Replace(match.Groups[1].Value, Guid.NewGuid().ToString());
      }
    }

    // Write the string back into a stream and parse			
    System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Export.PageTemplate));
    return Task.FromResult(serializer.Deserialize(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlData))) as Nucleus.Abstractions.Models.Export.PageTemplate);
  }

  private async Task CopyPermissionsToDescendants(Site site, PageMenu page, List<Permission> parentPermissions, CopyPermissionOperation operation)
  {
    List<Permission> newPermissions = [];

    //using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
    //{
    List<Permission> originalPermissions = await this.PermissionsManager.ListPermissions(page.Page.Id, Page.URN);
    switch (operation)
    {
      case CopyPermissionOperation.Replace:
        // this is the default behavior
        newPermissions = parentPermissions
          .Select(parentPermission => new Permission()
          {
            AllowAccess = parentPermission.AllowAccess,
            PermissionType = parentPermission.PermissionType,
            RelatedId = parentPermission.RelatedId,
            Role = parentPermission.Role
          }).ToList();
        break;

      case CopyPermissionOperation.Merge:
        // merge original permissions with new permissions
        newPermissions = parentPermissions
          .Select(parentPermission => new Permission()
          {
            AllowAccess = parentPermission.AllowAccess,
            PermissionType = parentPermission.PermissionType,
            RelatedId = parentPermission.RelatedId,
            Role = parentPermission.Role
          }).ToList();

        // Include original permissions where the role is not in parentPermissions
        newPermissions.AddRange(originalPermissions.Where
        (
          originalPermission => !newPermissions.Where
          (
            newPermission =>
            (
              newPermission.RelatedId == originalPermission.RelatedId &&
              newPermission.Role.Id == originalPermission.Role.Id &&
              newPermission.PermissionType.Scope.Equals(originalPermission.PermissionType.Scope, StringComparison.OrdinalIgnoreCase) &&
              newPermission.AllowAccess == originalPermission.AllowAccess
            )
          ).Any()
        ));

        break;
    }

    //await provider.SavePermissions(page.Page.Id, newPermissions, originalPermissions);
    await this.PermissionsManager.SavePermissions(page.Page.Id, newPermissions, originalPermissions);
    //}

    if (page.HasChildren)
    {
      foreach (PageMenu child in page.Children)
      {
        await CopyPermissionsToDescendants(site, child, parentPermissions, operation);
      }
    }

  }


  private string Encode(string value)
  {
    byte[] valueBytes = System.Text.Encoding.UTF8.GetBytes(value);
    return BitConverter.ToString(MD5.HashData(valueBytes)).Replace("-", "");
  }

  private async Task<PageMenuChildrenResult> GetPageMenuDescendants(Site site, Page parentPage, ClaimsPrincipal user, int levels, int thisLevel, PermissionsCheckOption permissionsCheckOption, Boolean ignoreDisabled, Boolean ignoreShowInMenu)
  {
    IList<Page> allPages = await this.ListSitePages(site);
    return await GetPageMenuDescendants(allPages, site, parentPage, user, levels, thisLevel, permissionsCheckOption, ignoreDisabled, ignoreShowInMenu);
  }

  private async Task<PageMenuChildrenResult> GetPageMenuDescendants(IList<Page> allPages, Site site, Page parentPage, ClaimsPrincipal user, int levels, int thisLevel, PermissionsCheckOption permissionsCheckOption, Boolean ignoreDisabled, Boolean ignoreShowInMenu)
  {
    List<PageMenu> children = [];

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      // read from database
      foreach (Page child in allPages.Where(childPage => childPage.ParentId == parentPage?.Id).OrderBy(page => page.SortOrder))
      {
        if (levels != 0 && thisLevel >= levels) return new PageMenuChildrenResult(true);

        if ((ignoreDisabled || !child.Disabled) && (ignoreShowInMenu || child.ShowInMenu) && HasPermission(site, child, user, permissionsCheckOption))
        {
          PageMenuChildrenResult childrenResult = await GetPageMenuDescendants(allPages, site, child, user, levels, thisLevel + 1, permissionsCheckOption, ignoreDisabled, ignoreShowInMenu);
          children.Add(new PageMenu(child, childrenResult.Children, childrenResult.HasChildren));
        }
      }
    }

    return new PageMenuChildrenResult(children);
  }

  private Boolean HasPermission(Site site, Page page, ClaimsPrincipal user, PermissionsCheckOption permissionsCheckOption)
  {
    return
      (!permissionsCheckOption.HasFlag(PermissionsCheckOption.View) || user.HasViewPermission(site, page)) &&
      (!permissionsCheckOption.HasFlag(PermissionsCheckOption.Edit) || user.HasEditPermission(site, page));
  }

  private class PageMenuChildrenResult
  {
    public Boolean HasChildren { get; set; }
    public IEnumerable<PageMenu> Children { get; set; }

    public PageMenuChildrenResult(Boolean hasChildren)
    {
      this.HasChildren = hasChildren;
    }

    public PageMenuChildrenResult(IEnumerable<PageMenu> children)
    {
      this.Children = children;
      this.HasChildren = this.Children?.Count() != 0;
    }
  }
}
