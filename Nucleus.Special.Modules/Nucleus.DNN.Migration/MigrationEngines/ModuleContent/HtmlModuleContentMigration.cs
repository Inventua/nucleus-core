using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Nucleus.Abstractions.FileSystemProviders;
using DocumentFormat.OpenXml.EMMA;
using System.Text.RegularExpressions;
using System.IO;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class HtmlModuleContentMigration : ModuleContentMigrationBase
{
  private ISiteManager SiteManager { get; }
  private IPageManager PageManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IContentManager ContentManager { get; }
  private DNNMigrationManager DnnMigrationManager { get; }

  private const string HREF_PREFIX_MATCH = "href[\\s]*=[\\s]*\"";
  private const string HREF_PREFIX = "href=\"";

  public HtmlModuleContentMigration(ISiteManager siteManager, IContentManager contentManager, IPageManager pageManager, IFileSystemManager fileSystemManager, DNNMigrationManager dnnMigrationManager)
  {
    this.SiteManager = siteManager;
    this.PageManager = pageManager;
    this.ContentManager = contentManager;
    this.FileSystemManager = fileSystemManager;
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override Guid ModuleDefinitionId => new("b516d8dd-c793-4776-be33-902eb704bef6");

  public override string ModuleFriendlyName => "Text/HTML";

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    return desktopModule.ModuleName.Equals("dnn_html", StringComparison.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage.SiteId);
    Models.DNN.Modules.TextHtml contentSource = await this.DnnMigrationManager.GetDnnHtmlContent(dnnModule.ModuleId);
    Nucleus.Abstractions.Models.Content content;
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    content = (await this.ContentManager.List(newModule)).FirstOrDefault();

    if (content == null)
    {
      content = new();
    }

    if (contentSource != null)
    {
      content.SortOrder = 10;
      content.ContentType = "text/html";
      content.Value = System.Web.HttpUtility.HtmlDecode(contentSource.Content);

      // rewrite url links like /LinkClick.aspx?link=54&tabid=166
      foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(content.Value, $"{HREF_PREFIX_MATCH}[/]{{0,1}}LinkClick\\.aspx\\?link=(?<tabid>[0-9]*)&[amp;]*tabid=(?<referrer_tabid>[0-9]*)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
      {
        content.Value = await ReplaceMatch(match, createdPagesKeys, content.Value);
      }

      // rewrite image src attributes like src="/portals/0/Images/instant-rebates.jpg?ver=Zo9cRKFqYNVRF84bLp4L0Q%3d%3d"
      // "src[\\s]*=[\\s]*[\"'][/]{0,1}\\/portals/[0-9]*/(?<path>[A-Za-z0-9-_ /.?=%]*)[\"']"
      foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(content.Value, $"src[\\s]*=[\\s]*[\"'][/]{0,1}(?<path>\\/portals/{dnnPage.PortalId}\\/[^\\s,'\"]*)[\"']", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
      {
        content.Value = await ReplaceFileMatch(dnnPage, dnnModule, site, fileSystemProvider, match, content.Value);
      }

      // rewrite srcset attributes
      // example: srcset="/portals/0/Images/fortune-award-footer-2023.jpg, /portals/0/images/fortune-award-footer-2023@2x.jpg 2x"
      foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(content.Value, $"srcset.*[\"'][\\/]{0,1}(?<path>\\/portals\\/{dnnPage.PortalId}\\/[^\\s,'\"]*)[\"']", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
      {
        content.Value = await ReplaceSrcSetMatch(dnnPage, dnnModule, site, fileSystemProvider, match, content.Value);
      }

      // rewrite url links like /?tabid=166
      foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(content.Value, $"{HREF_PREFIX_MATCH}[/]{{0,1}}\\?tabid=(?<tabid>[0-9]*)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
      {
        content.Value = await ReplaceMatch(match, createdPagesKeys, content.Value);
      }

      // rewrite url links like /Default.aspx?tabid=166
      foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(content.Value, $"{HREF_PREFIX_MATCH}[/]{{0,1}}Default\\.aspx\\?tabid=(?<tabid>[0-9]*)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
      {
        content.Value = await ReplaceMatch(match, createdPagesKeys, content.Value);
      }

      // attempt to rewrite url links for "friendly" urls which base the Url on the page name
      foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(content.Value, $"{HREF_PREFIX_MATCH}[/]{{0,1}}(?<pagename>[A-Za-z0-9-_% ]*)\\.(?<extension>[a-zA-Z]*)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
      {
        if (match.Success)
        {
          if (match.Groups.ContainsKey("pagename"))
          {
            Nucleus.Abstractions.Models.Page page = (await this.PageManager.List(site))
              .Where(foundPage => HomogenizeName(foundPage.Name).Equals(HomogenizeName(match.Groups["pagename"].Value), StringComparison.OrdinalIgnoreCase))
              .FirstOrDefault();

            if (page != null)
            {
              content.Value = ReplacePageMatch(match, page, content.Value);
            }
          }
        }
      }

      // apply "guesses" to the content to help with images/other file links.  This assumes that the contents of
      // /Portals/[index] will be copied in the same directory structure to Nucleus
      content.Value = content.Value.Replace($"/Portals/{dnnPage.PortalId}/", $"/{Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH}/{fileSystemProvider.Key}/", StringComparison.OrdinalIgnoreCase);

      await this.ContentManager.Save(newModule, content);
    }
  }

  /// <summary>
  /// Replace common word separators with "-"
  /// </summary>
  /// <param name="pageName"></param>
  /// <returns></returns>
  private string HomogenizeName(string pageName)
  {
    pageName = pageName.Replace("%20", "-");
    return System.Text.RegularExpressions.Regex.Replace(System.Web.HttpUtility.HtmlDecode(pageName), "[\\s_]", "-");
  }

  /// <summary>
  /// Replace the matched value (which represents the Url of a site page) with the Nucleus page Url for the page which
  /// was migrated from the tab id in the match.
  /// </summary>
  /// <param name="match"></param>
  /// <param name="createdPagesKeys"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  private async Task<string> ReplaceMatch(System.Text.RegularExpressions.Match match, Dictionary<int, Guid> createdPagesKeys, string value)
  {
    if (match.Success)
    {
      string tabId = GetTargetDnnTabId(match);

      if (!String.IsNullOrEmpty(tabId) && int.TryParse(tabId, out int parsedTabId))
      {
        if (createdPagesKeys.ContainsKey(parsedTabId))
        {
          Nucleus.Abstractions.Models.Page page = await this.PageManager.Get(createdPagesKeys[int.Parse(tabId)]);
          return ReplacePageMatch(match, page, value);
        }
      }
    }

    return value;
  }

  /// <summary>
  /// Replace the matched value (which represents a srcset attribute value) with the Nucleus file Urls for the files.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="providerKey"></param>
  /// <param name="match"></param>  
  /// <param name="value"></param>
  /// <returns></returns>
  private async Task<string> ReplaceSrcSetMatch(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Site site, FileSystemProviderInfo fileSystemProvider, System.Text.RegularExpressions.Match match, string value)
  {
    if (match.Success)
    {
      string[] targetParameterNames = { "path" };

      foreach (string parameterName in targetParameterNames)
      {
        if (match.Groups.ContainsKey(parameterName))
        {
          // Images/fortune-award-footer-2023.jpg, /portals/0/images/fortune-award-footer-2023@2x.jpg 2x
          string attrValue = match.Groups[parameterName].Value;
          foreach (System.Text.RegularExpressions.Match urlMatch in System.Text.RegularExpressions.Regex.Matches(attrValue, $"(?<path>\\/portals\\/{dnnPage.PortalId}\\/[^\\s,'\\\"]*)(?<size>[\\\\s0-9A-z]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
          {
            string original = GetFullFilePath(urlMatch);
            string path = GetLocalFilePath(urlMatch, dnnPage.PortalId.Value);
            Nucleus.Abstractions.Models.FileSystem.File linkedFile = await GetTargetFile(dnnPage, dnnModule, site, fileSystemProvider.Key, path);

            if (linkedFile != null)
            {
              return value.Replace(original, $"/files/{linkedFile.Provider}/{linkedFile.Path}", StringComparison.OrdinalIgnoreCase);
            }
          }
        }
      }
    }

    return value;
  }


  /// <summary>
  /// Replace the matched value (which represents a file Url of a site page) with the Nucleus file Url for the file.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="providerKey"></param>
  /// <param name="match"></param>  
  /// <param name="value"></param>
  /// <returns></returns>
  private async Task<string> ReplaceFileMatch(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Site site, FileSystemProviderInfo fileSystemProvider, System.Text.RegularExpressions.Match match, string value)
  {
    if (match.Success)
    {
      string original = GetFullFilePath(match);
      string path = GetLocalFilePath(match, dnnPage.PortalId.Value);
      Nucleus.Abstractions.Models.FileSystem.File linkedFile = await GetTargetFile(dnnPage, dnnModule, site, fileSystemProvider.Key, path);

      if (linkedFile != null)
      {
        return value.Replace(original, $"/files/{linkedFile.Provider}/{linkedFile.Path}", StringComparison.OrdinalIgnoreCase);
      }
    }

    return value;
  }

  /// <summary>
  /// Replace the matched value (which represents the Url of a site page) with the Nucleus page Url
  /// </summary>
  /// <param name="match"></param>
  /// <param name="page"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  private string ReplacePageMatch(System.Text.RegularExpressions.Match match, Nucleus.Abstractions.Models.Page page, string value)
  {
    string path = HREF_PREFIX + page.DefaultPageRoute().Path;
    path += path.EndsWith("/") ? "" : "/";
    path += "\"";

    return value.Replace(match.Value, path, StringComparison.OrdinalIgnoreCase);
  }

  private string GetTargetDnnTabId(System.Text.RegularExpressions.Match match)
  {
    string[] targetParameterNames = { "tabid" };

    foreach (string parameterName in targetParameterNames)
    {
      if (match.Groups.ContainsKey(parameterName))
      {
        return match.Groups[parameterName].Value;
      }
    }

    return null;
  }

  private string GetFullFilePath(System.Text.RegularExpressions.Match match)
  {
    string[] targetParameterNames = { "path" };

    foreach (string parameterName in targetParameterNames)
    {
      if (match.Groups.ContainsKey(parameterName))
      {
        return match.Groups[parameterName].Value;
      }
    }

    return null;
  }

  private string GetLocalFilePath(System.Text.RegularExpressions.Match match, int portalId)
  {
    string[] targetParameterNames = { "path" };

    foreach (string parameterName in targetParameterNames)
    {
      if (match.Groups.ContainsKey(parameterName))
      {
        return System.Text.RegularExpressions.Regex.Replace(match.Groups[parameterName].Value, $"\\/portals\\/{portalId}\\/", "");
      }
    }

    return null;
  }

  private async Task<Nucleus.Abstractions.Models.FileSystem.File> GetTargetFile(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Site site, string providerKey, string urlValue)
  {
    if (urlValue == null) return null;

    try
    {
      if (urlValue.Contains('?'))
      {
        urlValue = urlValue.Substring(0, urlValue.IndexOf('?') - 1);
      }
      return await this.FileSystemManager.GetFile(site, providerKey, urlValue);
    }
    catch (FileNotFoundException)
    {
      dnnPage.AddWarning($"The HTML module with title '{dnnModule.ModuleTitle}' contains a link to a missing file '{urlValue}'. This link was not replaced, and will be a broken link.");
    }

    return null;
  }
}
