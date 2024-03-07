using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Nucleus.Core.Managers;

/// <summary>
/// Provides functions to manage database data for <see cref="Site"/>s and <see cref="SiteAlias"/>es.
/// </summary>
public partial class SiteManager : ISiteManager
{
  // "partial" class is required by GeneratedRegex

  [GeneratedRegexAttribute(@"({\$guid[0-9]+})", RegexOptions.ECMAScript)]
  private static partial System.Text.RegularExpressions.Regex GuidTokenRegEx();

  [GeneratedRegex("^[\n]*", RegexOptions.Singleline)]
  private static partial Regex LeadingNewlinesRegEx();

  [GeneratedRegex("[\n]*$", RegexOptions.Singleline)]
  private static partial Regex TrailingNewLinesRegEx();

  [GeneratedRegex(@"<(.+)>\s*<(.+)>([\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12})", RegexOptions.ECMAScript)]
  private static partial Regex FindGuidRegEx();

  private IDataProviderFactory DataProviderFactory { get; }
  private ICacheManager CacheManager { get; }
  private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }
  private IPermissionsManager PermissionsManager { get; }

  private long _siteCount = -1;

  public SiteManager(IDataProviderFactory dataProviderFactory, IPermissionsManager permissionsManager, ICacheManager cacheManager, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
  {
    this.CacheManager = cacheManager;
    this.PermissionsManager = permissionsManager;
    this.DataProviderFactory = dataProviderFactory;
    this.FolderOptions = folderOptions;
  }

  /// <summary>
  /// Create a new <see cref="Site"/> with default values.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// This function does not save the new <see cref="Site"/> to the database.  Call <see cref="Save(Site, Site)"/> to save the role group.
  /// </remarks>
  public Task<Site> CreateNew()
  {
    Site result = new();

    return Task.FromResult(result);
  }

  /// <summary>
  /// Retrieve an existing <see cref="Site"/> from the database.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public async Task<Site> Get(Guid id)
  {
    return await this.CacheManager.SiteCache().GetAsync(id, async id =>
    {
      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
      {
        return await provider.GetSite(id);
      }
    });
  }


  /// <summary>
  /// Retrieve an existing <see cref="Site"/> from the database which has a <see cref="SiteAlias"/> which matches the specified 
  /// requestUri and pathBase.
  /// </summary>
  /// <param name="requestUri"></param>
  /// <param name="pathBase"></param>
  /// <returns></returns>
  public async Task<Site> Get(Microsoft.AspNetCore.Http.HostString requestUri, string pathBase)
  {
    string siteDetectCacheKey = (requestUri + "^" + pathBase).ToLower();

    Guid id = await this.CacheManager.SiteDetectCache().GetAsync(siteDetectCacheKey, async siteDetectCacheKey =>
    {
      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
      {
        return await provider.DetectSite(requestUri, pathBase);
      }
    });

    return await this.Get(id);
  }

  /// <summary>
  /// Retrieve the existing <see cref="Site"/> from the database which contains the specified page.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public async Task<Site> Get(Page page)
  {
    return await Get(page.SiteId);
  }

  /// <summary>
  /// Delete the specified <see cref="Site"/> from the database.
  /// </summary>
  /// <param name="site"></param>
  public async Task Delete(Site site)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      await provider.DeleteSite(site);
      this.CacheManager.SiteCache().Remove(site.Id);
      ResetSiteCount();
    }
  }

  private void ResetSiteCount()
  {
    _siteCount = -1;
  }

  /// <summary>
  /// Get the specified <see cref="SiteAlias"/> from the database.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public async Task<SiteAlias> GetAlias(Guid id)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      return await provider.GetSiteAlias(id);
    }
  }

  /// <summary>
  /// Returns an existing <see cref="UserProfileProperty"/>
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public async Task<UserProfileProperty> GetUserProfileProperty(Guid id)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      return await provider.GetUserProfileProperty(id);
    }
  }

  /// <summary>
  /// Update the <see cref="UserProfileProperty.SortOrder"/> of the user profile property specifed by id by swapping it with the next-highest <see cref="UserProfileProperty.SortOrder"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="id"></param>
  public async Task MoveUserProfilePropertyDown(Site site, Guid id)
  {
    UserProfileProperty previousProp = null;
    UserProfileProperty thisProp;

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      thisProp = await this.GetUserProfileProperty(id);

      List<UserProfileProperty> properties = await provider.ListSiteUserProfileProperties(site.Id);

      properties.Reverse();
      foreach (UserProfileProperty prop in properties)
      {
        if (prop.Id == id)
        {
          if (previousProp != null)
          {
            int temp = prop.SortOrder;
            prop.SortOrder = previousProp.SortOrder;
            previousProp.SortOrder = temp;

            await provider.SaveUserProfileProperty(site.Id, previousProp);
            await provider.SaveUserProfileProperty(site.Id, prop);

            site.UserProfileProperties = await provider.ListSiteUserProfileProperties(site.Id);

            // Properties are cached as part of their site, so we have invalidate the cache for the site
            this.CacheManager.SiteCache().Remove(site.Id);

            // User properties are cached as part of user proerty values, so we have to invalidate the cache
            // for ALL users when a site's user properties change
            this.CacheManager.UserCache().Clear();

            break;
          }
        }
        else
        {
          previousProp = prop;
        }
      }
    }
  }

  /// <summary>
  /// Update the <see cref="UserProfileProperty.SortOrder"/> of the user profile property specifed by id by swapping it with the previous <see cref="UserProfileProperty.SortOrder"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="id"></param>
  public async Task MoveUserProfilePropertyUp(Site site, Guid id)
  {
    UserProfileProperty previousProp = null;
    UserProfileProperty thisProp;

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      thisProp = await provider.GetUserProfileProperty(id);

      List<UserProfileProperty> properties = await provider.ListSiteUserProfileProperties(site.Id);

      foreach (UserProfileProperty prop in properties)
      {
        if (prop.Id == id)
        {
          if (previousProp != null)
          {
            int temp = prop.SortOrder;
            prop.SortOrder = previousProp.SortOrder;
            previousProp.SortOrder = temp;

            await provider.SaveUserProfileProperty(site.Id, previousProp);
            await provider.SaveUserProfileProperty(site.Id, prop);

            site.UserProfileProperties = await provider.ListSiteUserProfileProperties(site.Id);

            // Properties are cached as part of their site, so we have invalidate the cache for the site
            this.CacheManager.SiteCache().Remove(site.Id);

            // User properties are cached as part of user proerty values, so we have to invalidate the cache
            // for ALL users when a site's user properties change
            this.CacheManager.UserCache().Clear();

            break;
          }
        }
        else
        {
          previousProp = prop;
        }
      }
    }
  }

  /// <summary>
  /// Count the System Administrator <see cref="User"/>s.
  /// </summary>
  /// <param name="site"></param>
  /// <returns></returns>
  public async Task<long> Count()
  {
    if (_siteCount < 0)
    {
      using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
      {
        _siteCount = await provider.CountSites();
      }
    }
    return _siteCount;
  }

  /// <summary>
  /// List all <see cref="Site"/>s.
  /// </summary>
  /// <returns></returns>
  public async Task<IList<Site>> List()
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      return await provider.ListSites();
    }
  }

  /// <summary>
  /// Create or update the specified <see cref="Site"/>.
  /// </summary>
  /// <param name="site"></param>
  public async Task Save(Site site)
  {
    if (System.IO.Path.IsPathRooted(site.HomeDirectory))
    {
      throw new ArgumentException($"'{site.HomeDirectory}' is not a valid home directory.  The home directory must not start with a backslash or a drive letter.");
    }

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      await provider.SaveSite(site);
      this.CacheManager.SiteCache().Remove(site.Id);
      this.CacheManager.SiteDetectCache().Clear();
      ResetSiteCount();
    }
  }

  /// <summary>
  /// Create or update the specified <see cref="SiteAlias"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="siteAlias"></param>
  public async Task SaveAlias(Site site, SiteAlias siteAlias)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      await provider.SaveSiteAlias(site.Id, siteAlias);
      this.CacheManager.SiteCache().Remove(site.Id);
      this.CacheManager.SiteDetectCache().Clear();
    }
  }

  /// <summary>
  /// Delete the specified <see cref="SiteAlias"/> from the database.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="id"></param>
  public async Task DeleteAlias(Site site, Guid id)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      await provider.DeleteSiteAlias(site.Id, id);
      this.CacheManager.SiteCache().Remove(site.Id);
    }
  }

  /// <summary>
  /// Create or update the specified <see cref="UserProfileProperty"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="property"></param>
  public async Task SaveUserProfileProperty(Site site, UserProfileProperty property)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      await provider.SaveUserProfileProperty(site.Id, property);
      this.CacheManager.SiteCache().Remove(site.Id);

      // User properties are cached as part of user proerty values, so we have to invalidate the cache
      // for ALL users when a site's user properties change
      this.CacheManager.UserCache().Clear();
    }
  }

  /// <summary>
  /// Delete the specified <see cref="UserProfileProperty"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="id"></param>
  public async Task DeleteUserProfileProperty(Site site, Guid id)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      await provider.DeleteUserProfileProperty(await provider.GetUserProfileProperty(id));
      this.CacheManager.SiteCache().Remove(site.Id);
    }
  }

  /// <summary>
  /// Export the specified <see cref="Site"/> as XML.
  /// </summary>
  /// <param name="site"></param>
  /// <remarks>
  /// The main purpose for this function is to create site templates.  
  /// 
  /// Site groups, users, scheduled tasks, folders, files and installable items like modules, containers and layouts 
  /// are not included in the export.  Simple module settings stored in <see cref="PageModule.ModuleSettings"/> are
  /// included in the template, but any data stored in module-specific tables is not included.
  /// </remarks>
  public async Task<System.IO.Stream> Export(Site site)
  {
    string xmlData;

    Nucleus.Abstractions.Models.Export.SiteTemplate export = new()
    {
      Site = site
    };

    using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
    {
      export.PermissionTypes = await provider.ListPermissionTypes(Page.URN);
      export.PermissionTypes.AddRange(await provider.ListPermissionTypes(PageModule.URN));
      export.PermissionTypes.AddRange(await provider.ListPermissionTypes(Nucleus.Abstractions.Models.FileSystem.Folder.URN));
    }

    using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
    {
      export.ScheduledTasks = await provider.ListScheduledTasks();
    }

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      // when exporting, we need to sort the pages by their parent relationships so that they are created in the 
      // right order.
      //export.Pages = provider.ListPages(site.Id);
      export.Pages = await GetPagesSortedByHierachy(site, null);
    }

    using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
    {
      export.Contents = [];
      foreach (PageModule pageModule in export.Pages.SelectMany(page => page.Modules))
      {
        export.Contents.AddRange(await provider.ListContent(pageModule));
      }
    }

    using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
    {
      export.Lists = new(await provider.ListLists(site));
    }

    using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
    {
      export.RoleGroups = new(await provider.ListRoleGroups(site));

      // don't include special site roles as these are imported from the Sites entity
      export.Roles = (await provider.ListRoles(site))
        .Where(role => role != site.RegisteredUsersRole && role != site.AdministratorsRole && role != site.AllUsersRole && role != site.AnonymousUsersRole).ToList();
    }

    using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
    {
      export.MailTemplates = new(await provider.ListMailTemplates(site));
    }

    // serialize the export data to XML
    using (System.IO.MemoryStream serializedSite = new())
    {
      System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Export.SiteTemplate));
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
          case nameof(LayoutDefinition):   // nameof(Page.LayoutDefinition):
          case nameof(Site.DefaultLayoutDefinition):
          case nameof(Site.DefaultContainerDefinition):  // nameof(Page.DefaultContainer), case nameof(Site.DefaultContainer),	nameof(PageModule.ContainerDefinition)					
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

  private async Task<List<Page>> GetPagesSortedByHierachy(Site site, Page parentPage)
  {
    List<Page> pages = [];

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      // read from database
      foreach (Page child in await provider.ListPages(site.Id, parentPage?.Id))
      {
        pages.Add(child);
        pages.AddRange(await GetPagesSortedByHierachy(site, child));
      }
    }

    return pages;
  }


  /// <summary>
  /// Import a site template, creating a new site in the process.
  /// </summary>
  /// <param name="template"></param>
  public async Task<Site> Import(Nucleus.Abstractions.Models.Export.SiteTemplate template)
  {
    using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
    {
      foreach (PermissionType permissionType in template.PermissionTypes)
      {
        await provider.AddPermissionType(permissionType);
      }
    }

    using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
    {
      foreach (ScheduledTask task in template.ScheduledTasks)
      {
        // only add scheduled task if it is not already present
        if (await provider.GetScheduledTaskByTypeName(task.TypeName) == null)
        {
          await provider.SaveScheduledTask(task);
        }
      }
    }

    await this.Save(template.Site);
    await this.SaveAlias(template.Site, template.Site.DefaultSiteAlias);

    foreach (UserProfileProperty property in template.Site.UserProfileProperties)
    {
      await SaveUserProfileProperty(template.Site, property);
    }

    using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
    {
      foreach (List list in template.Lists)
      {
        await provider.SaveList(template.Site, list);
      }
    }

    using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
    {
      foreach (RoleGroup group in template.RoleGroups)
      {
        await provider.SaveRoleGroup(template.Site, group);
      }

      await provider.SaveRole(template.Site, template.Site.AdministratorsRole);
      await provider.SaveRole(template.Site, template.Site.AllUsersRole);
      await provider.SaveRole(template.Site, template.Site.AnonymousUsersRole);
      await provider.SaveRole(template.Site, template.Site.RegisteredUsersRole);

      foreach (Role role in template.Roles)
      {
        await provider.SaveRole(template.Site, role);
      }
    }

    using (IMailDataProvider provider = this.DataProviderFactory.CreateProvider<IMailDataProvider>())
    {
      foreach (Nucleus.Abstractions.Models.Mail.MailTemplate mailTemplate in template.MailTemplates)
      {
        mailTemplate.Body = TrimStrings(mailTemplate.Body);
        await provider.SaveMailTemplate(template.Site, mailTemplate);
      }
    }

    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      foreach (Page page in template.Pages)
      {
        await provider.SavePage(template.Site, page);
        await this.PermissionsManager.SavePermissions(page.Id, page.Permissions, new List<Permission>());

        foreach (PageModule module in page.Modules)
        {
          await provider.SavePageModule(page.Id, module);
          await this.PermissionsManager.SavePermissions(module.Id, module.Permissions, new List<Permission>());
        }
      }
    }

    using (IContentDataProvider provider = this.DataProviderFactory.CreateProvider<IContentDataProvider>())
    {
      foreach (Content content in template.Contents)
      {
        PageModule module;
        using (ILayoutDataProvider layoutProvider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
        {
          module = await layoutProvider.GetPageModule(content.PageModuleId);
        }
        await provider.SaveContent(module, content);
      }
    }


    // We have to save twice, because the site aliases aren't created until AFTER the site is created, so we
    // can't set the DefaultSiteAlias on add.  Same for roles, they aren't added until after the site has been created.
    // So we need to call .Save again in order to do an update, to set the default alias and system roles.
    await this.Save(template.Site);

    return template.Site;
  }

  /// <summary>
  /// Parse an export file and return the deserialized result
  /// </summary>
  /// <param name="stream"></param>
  /// <returns></returns>
  public Task<Nucleus.Abstractions.Models.Export.SiteTemplate> ParseTemplate(System.IO.Stream stream)
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
    System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Export.SiteTemplate));
    return Task.FromResult(serializer.Deserialize(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlData))) as Nucleus.Abstractions.Models.Export.SiteTemplate);
  }

  public Task<string> SaveTemplateTempFile(Nucleus.Abstractions.Models.Export.SiteTemplate template)
  {
    string templateTempFileName = Guid.NewGuid().ToString();
    string templateTempFileFullName = System.IO.Path.Combine(this.FolderOptions.Value.GetTempFolder(), templateTempFileName);

    System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Export.SiteTemplate));
    using (System.IO.Stream stream = System.IO.File.OpenWrite(templateTempFileFullName))
    {
      serializer.Serialize(stream, template);
    }

    return Task.FromResult(templateTempFileName);
  }

  public Task<Nucleus.Abstractions.Models.Export.SiteTemplate> ReadTemplateTempFile(string templateTempFileName)
  {
    string templateTempFileFullName = System.IO.Path.Combine(this.FolderOptions.Value.GetTempFolder(), templateTempFileName);

    System.Xml.Serialization.XmlSerializer serializer = new(typeof(Nucleus.Abstractions.Models.Export.SiteTemplate));
    using (System.IO.Stream stream = System.IO.File.OpenRead(templateTempFileFullName))
    {
      return Task.FromResult(serializer.Deserialize(stream) as Nucleus.Abstractions.Models.Export.SiteTemplate);
    }
  }

  /// <summary>
  /// String values parsed from XML can contain additional spaces and CR/LF characters - remove them.
  /// </summary>
  /// <param name="content"></param>
  private static string TrimStrings(string content)
  {
    string result = content;

    // Remove new lines from the start
    result = LeadingNewlinesRegEx().Replace(result, "");

    // Remove new lines from the end
    result = TrailingNewLinesRegEx().Replace(result, "");

    //// Treat tabs as two spaces
    //result = result.Replace("\t", "  ");

    // Count the whitespace characters at the start of the first line
    string firstLine = result.Split(['\r', '\n']).FirstOrDefault();
    if (firstLine == null) return result;

    // Count the spaces
    int spaceCount = firstLine.TakeWhile(Char.IsWhiteSpace).Count();

    //// Remove leading spaces 
    //result = System.Text.RegularExpressions.Regex.Replace(result, $"^[ ]{{{spaceCount}}}", "", System.Text.RegularExpressions.RegexOptions.Multiline);

    // https://stackoverflow.com/questions/25954446/remove-extra-whitespaces-but-keep-new-lines-using-a-regular-expression-in-c-sha
    // Removes the number of leading spaces depending on the first line but leaves extra spaces/tab/newlines intact
    result = System.Text.RegularExpressions.Regex.Replace(result, $@"^[^\S\r\n\t]{{0,{spaceCount}}}", "", System.Text.RegularExpressions.RegexOptions.Multiline);

    return result;
  }
}
