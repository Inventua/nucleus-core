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
using Nucleus.ViewFeatures;
using Nucleus.XmlDocumentation.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Nucleus.XmlDocumentation
{
  public class ApiMetaDataProducer : IContentMetaDataProducer
  {
    private IFileSystemManager FileSystemManager { get; }
    private IExtensionManager ExtensionManager { get; }
    private IPageManager PageManager { get; }
    private IPageModuleManager PageModuleManager { get; }
    private IApiKeyManager ApiKeyManager { get; }
    private HttpClient HttpClient { get; }

    private ILogger<ApiMetaDataProducer> Logger { get; }
    private ICacheManager CacheManager { get; }

    public ApiMetaDataProducer(HttpClient httpClient, IApiKeyManager apiKeyManager, ISiteManager siteManager, IPageManager pageManager, IPageModuleManager pageModuleManager, ICacheManager cacheManager, IFileSystemManager fileSystemManager, IExtensionManager extensionManager, ILogger<ApiMetaDataProducer> logger)
    {
      this.ApiKeyManager = apiKeyManager;
      this.HttpClient = httpClient;
      this.FileSystemManager = fileSystemManager;
      this.ExtensionManager = extensionManager;
      this.PageManager = pageManager;
      this.PageModuleManager = pageModuleManager;
      this.CacheManager = cacheManager;
      this.Logger = logger;
    }

    public async override IAsyncEnumerable<ContentMetaData> ListItems(Site site)
    {
      // This must match the value in package.xml
      Guid moduleDefinitionId = Guid.Parse("0595de3d-1ba7-49df-99f0-2e80fb00d156");
      ApiKey apiKey = null;

      if (site.DefaultSiteAlias == null)
      {
        this.Logger.LogWarning("Site {0} skipped because it does not have a default alias.", site.Id);
      }
      else
      {
        if (site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.APIKEY_ID, out Guid result))
        {
          apiKey = await this.ApiKeyManager.Get(result);
        }

        foreach (PageModule module in await this.ExtensionManager.ListPageModules(new Nucleus.Abstractions.Models.ModuleDefinition() { Id = moduleDefinitionId }))
        {
          Page page = await this.PageManager.Get(module.PageId);

          if (!page.IncludeInSearch)
          {
            Logger?.LogInformation("Skipping XMLDocumentation module on page {pageid}/{pagename} because the page's 'Include in search' setting is false.", page.Id, page.Name);
          }
          else
          {
            foreach (Models.ApiDocument document in await GetApiDocuments(site, module))
            {
              await foreach (ContentMetaData item in BuildContent(site, apiKey, page, module, document))
              {
                if (item != null)
                {
                  yield return item;
                }
              }
            }
          }
        }
      }
    }

    private async IAsyncEnumerable<ContentMetaData> BuildContent(Site site, ApiKey apiKey, Page page, PageModule module, Models.ApiDocument document)
    {
      site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.INDEX_PAGES_USE_SSL, out Boolean useSsl);

      yield return (await BuildContentMetaData(site, apiKey, page, module, document, useSsl));

      foreach (Models.ApiClass apiClass in document.Classes)
      {
        yield return await BuildContentMetaData(site, apiKey, page, module, document, apiClass, useSsl);
      }
    }

    /// <summary>
    /// Return a meta-data entry for the Api class meta-data
    /// </summary>
    /// <param name="site"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    private async Task<ContentMetaData> BuildContentMetaData(Site site, ApiKey apiKey, Page page, PageModule module, Models.ApiDocument document, Boolean useSsl)
    {
      if (page != null)
      {
        ContentMetaData documentContentItem = new()
        {
          Site = site,
          Title = $"{document.Namespace.Name} Namespace",
          Summary = RenderMixedContent(document.Namespace.Summary),
          Keywords = document.Namespace.Name.Split('.', StringSplitOptions.RemoveEmptyEntries).Concat(new string[] { document.AssemblyName, document.Namespace.Name }),
          Url = document.GenerateUrl(page),
          PublishedDate = document.LastModifiedDate,
          SourceId = null,
          Scope = Models.ApiDocument.URN,
          Type = "API Documentation",
          Roles = await GetViewRoles(module),
          ContentType = "text/html"
        };

        // include the linked file as content
        await GetContent(site, apiKey, documentContentItem, useSsl);

        return documentContentItem;
      }

      return null;
    }

    /// <summary>
    /// Return a meta-data entry for the Api class meta-data
    /// </summary>
    /// <param name="site"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    private async Task<ContentMetaData> BuildContentMetaData(Site site, ApiKey apiKey, Page page, PageModule module, Models.ApiDocument document, Models.ApiClass apiClass, Boolean useSsl)
    {
      if (page != null)
      {
        ContentMetaData documentContentItem = new()
        {
          Site = site,
          Title = $"{apiClass.Name} {apiClass.Type}",
          Summary = RenderMixedContent(apiClass.Summary),
          Keywords = apiClass.FullName.Split('.', StringSplitOptions.RemoveEmptyEntries).Concat(new string[] { apiClass.FullName, apiClass.AssemblyName, apiClass.Namespace }),
          Url = apiClass.GenerateUrl(page, document),
          PublishedDate = document.LastModifiedDate,
          SourceId = null,
          Scope = Models.ApiClass.URN,
          Type = "API Documentation",
          Roles = await GetViewRoles(module),
          ContentType = "text/html"
        };

        // include the linked file as content
        await GetContent(site, apiKey, documentContentItem, useSsl);

        return documentContentItem;
      }

      return null;
    }

    private string RenderMixedContent(Models.Serialization.MixedContent content)
    {
      if (content?.Items == null || !content.Items.Any()) return "";

      StringBuilder result = new();

      foreach (var item in content.Items)
      {
        if (item is String)
        {
          result.AppendLine(item as String);
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.See)
        {
          var seeItem = item as Nucleus.XmlDocumentation.Models.Serialization.See;
          if (!String.IsNullOrEmpty(seeItem.Keyword))
          {
            result.AppendLine(seeItem.Keyword);
          }
          else
          {
            if (!String.IsNullOrEmpty(seeItem.LinkText))

            {
              result.AppendLine(seeItem.LinkText);
            }
            else
            {
              result.AppendLine(seeItem.CodeReference);
            }
          }
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.SeeAlso)
        {
          var seeItem = item as Nucleus.XmlDocumentation.Models.Serialization.SeeAlso;
          if (!String.IsNullOrEmpty(seeItem.LinkText))
          {
            result.AppendLine(seeItem.LinkText);
          }
          else
          {
            result.AppendLine(seeItem.CodeReference);
          }
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.ParamRef)
        {
          result.AppendLine(((item as Nucleus.XmlDocumentation.Models.Serialization.ParamRef).Name));
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.TypeParamRef)
        {
          result.AppendLine(((item as Nucleus.XmlDocumentation.Models.Serialization.TypeParamRef).Name));
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.Value)
        {
          result.AppendLine(((item as Nucleus.XmlDocumentation.Models.Serialization.Value).Description));
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.Paragraph)
        {
          result.AppendLine(((item as Nucleus.XmlDocumentation.Models.Serialization.Paragraph).Description));
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.Code)
        {
          result.AppendLine(((item as Nucleus.XmlDocumentation.Models.Serialization.Code).Description));
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.InlineCode)
        {
          result.AppendLine(((item as Nucleus.XmlDocumentation.Models.Serialization.Code).Description));
        }
        else if (item is Nucleus.XmlDocumentation.Models.Serialization.Note)
        {
          result.AppendLine(((item as Nucleus.XmlDocumentation.Models.Serialization.Note).Description));
        }
      }

      return result.ToString().Trim();

      // Using the Razor parser does not work
      //try
      //{
      //	// This code runs outside the context of a request, so we have to specify the full relative path of the template file.
      //	string template = System.IO.File.ReadAllText("Extensions/XmlDocumentation/Views/_RenderMixedContentTemplate.cshtml");
      //	return (await Nucleus.Extensions.Razor.RazorParser.Parse<Models.Serialization.MixedContent>(template, content)).Trim();
      //}
      //catch(Exception ex)
      //{
      //	// If the Razor parser can't generate a result, return an empty string and log the error so that any issues don't
      //	// prevent the item from being added to search content.
      //	this.Logger?.LogError(ex, "Rendering summary content.");
      //	return "";
      //}
    }

    private async Task GetContent(Site site, ApiKey apiKey, ContentMetaData contentItem, Boolean useSsl)
    {
      System.IO.MemoryStream htmlContent = new();
      Uri uri = new(new Uri((useSsl ? "https" : "http") + Uri.SchemeDelimiter + site.DefaultSiteAlias.Alias), contentItem.Url + (contentItem.Url.Contains('?') ? '&' : '?') + "showapimenu=false");

      System.Net.Http.HttpRequestMessage request = new(HttpMethod.Get, uri);

      if (apiKey != null)
      {
        request.Headers.Host = site.DefaultSiteAlias.Alias;
        request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Nucleus-Search-Feeder", this.GetType().Assembly.GetName().Version.ToString()));
        request.Sign(apiKey.Id, apiKey.Secret);
      }

      // Signal Nucleus to only render module content
      request.Headers.Add("X-Nucleus-OverrideLayout", "ContentOnly");

      System.Net.Http.HttpResponseMessage response = this.HttpClient.Send(request);

      if (!response.IsSuccessStatusCode)
      {
        // response was an error, use page meta-data only
      }
      else
      {
        using (System.IO.Stream responseStream = await response.Content.ReadAsStreamAsync())
        {
          // Kestrel doesn't return a content-length, so we have to read into a memory stream first in order to determine the 
          // size of the content array.
          await responseStream.CopyToAsync(htmlContent);
          responseStream.Close();
        }

        contentItem.ContentType = "text/html";

        contentItem.Size = htmlContent.Length;

        htmlContent.Position = 0;
        contentItem.Content = htmlContent.ToArray();
        htmlContent.Close();
      }
    }

    private async Task<List<Role>> GetViewRoles(Folder folder)
    {
      return
        (await this.FileSystemManager.ListPermissions(folder))
          .Where(permission => permission.AllowAccess && permission.IsFolderViewPermission())
          .Select(permission => permission.Role).ToList();
    }

    private async Task<List<Role>> GetViewRoles(PageModule module)
    {
      return
        (await this.PageModuleManager.ListPermissions(module))
          .Where(permission => permission.AllowAccess && permission.IsModuleViewPermission())
          .Select(permission => permission.Role).ToList();
    }

    private async Task<List<ApiDocument>> GetApiDocuments(Site site, PageModule module)
    {
      return await this.CacheManager.XmlDocumentationCache().GetAsync(module.Id, async id =>
      {
        List<ApiDocument> results = new();
        Folder documentationFolder;

        try
        {
          documentationFolder = await this.FileSystemManager.ListFolder(site, module.ModuleSettings.Get(Nucleus.XmlDocumentation.Controllers.XmlDocumentationController.MODULESETTING_DOCUMENTATION_FOLDER_ID, Guid.Empty), "(.xml)");

          // parse the documentation file, render results
          try
          {

            foreach (File xmlDocument in documentationFolder.Files)
            {
              DocumentationParser parser = new(this.FileSystemManager, site, xmlDocument);
              if (parser.IsValid)
              {
                results.Add(parser.Document);
              }
            }

            DocumentationParser.ParseParams(results);
            DocumentationParser.ParseMixedContent(results);
          }
          catch (System.Exception e)
          {
            this.Logger.LogError(e, "");
            results = new();
          }
        }
        catch (System.IO.FileNotFoundException e)
        {
          this.Logger.LogError(e, "Documentation folder not set.");
          results = new();
        }

        return results;
      });
    }
  }
}
