using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Extensions;

/// <summary>
/// Extension methods to transform <see cref="Site"/> <see cref="Site.SiteSettings"/> from name/value pairs to strongly-typed classes and back again.
/// </summary>
public static class SiteExtensions
{  
  /// <summary>
  /// Returns a value which specifies whether the site's user profile values contains a property with the specified 
  /// <paramref name="typeUri"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="typeUri"></param>
  /// <returns></returns>
  static public Boolean HasProperty(this Site site, string typeUri)
  {
    return site.UserProfileProperties.Exists(prop => prop.TypeUri == typeUri);
  }

  /// <summary>
  /// Sets <see cref="Site.SiteSettings"/> based on a <see cref="SiteTemplateSelections"/> object.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="siteTemplateSelections"></param>
  public static void SetSiteMailTemplates(this Site site, SiteTemplateSelections siteTemplateSelections)
  {
    site.SiteSettings.TrySetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_WELCOMENEWUSER, siteTemplateSelections.WelcomeNewUserTemplateId);
    site.SiteSettings.TrySetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_PASSWORDRESET, siteTemplateSelections.PasswordResetTemplateId);
    site.SiteSettings.TrySetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_ACCOUNTNAMEREMINDER, siteTemplateSelections.AccountNameReminderTemplateId);
  }

  /// <summary>
  /// Gets a <see cref="SiteTemplateSelections"/> object based on  <see cref="Site.SiteSettings"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <returns></returns>
  public static SiteTemplateSelections GetSiteTemplateSelections(this Site site)
  {
    SiteTemplateSelections result = new();

    if (site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_WELCOMENEWUSER, out Guid id))
    {
      result.WelcomeNewUserTemplateId = id;
    }

    if (site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_PASSWORDRESET, out id))
    {
      result.PasswordResetTemplateId = id;
    }

    if (site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_ACCOUNTNAMEREMINDER, out id))
    {
      result.AccountNameReminderTemplateId = id;
    }

    return result;
  }

  /// <summary>
  /// Return the relative path to the site's icon image file.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="fileSystemManager"></param>
  /// <returns></returns>
  public async static Task<string> GetIconPath(this Site site, IFileSystemManager fileSystemManager)
  {
    if (site.SiteSettings.TryGetValue(Site.SiteFilesKeys.FAVICON_FILEID, out Guid fileId))
    {
      if (fileId == Guid.Empty)
      {
        return "";
      }
      else
      {
        try
        {
          return await GetDirectFilePath(site, fileId, fileSystemManager);
        }
        catch (System.IO.FileNotFoundException)
        {
          // file not found
          return "";
        }
      }
    }
    return null;
  }

  /// <summary>
  /// Return the site's selected mail client.
  /// </summary>
  /// <param name="site"></param>
  /// <returns>
  /// The type name of the site's selected mail client, or null if no mail client is selected.
  /// </returns>
  public static string GetMailClientType(this Site site)
  {
    if (site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_CLIENT_TYPENAME, out string typeName))
    {
      if (String.IsNullOrEmpty(typeName))
      {
        return null;
      }
      else
      {
        return typeName;
      }
    }
    return null;
  }

  /// <summary>
  /// Set the site's selected mail client.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="mailClientType">The .NET type name (class and and assembly) of the mail client.</param>
  public static void SetMailClientType(this Site site, string mailClientType)
  {
    site.SiteSettings.TrySetValue(Site.SiteMailSettingKeys.MAIL_CLIENT_TYPENAME, mailClientType);
  }

  /// <summary>
  /// Return the relative path to the site's icon image file.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="fileSystemManager"></param>
  /// <returns></returns>
  public async static Task<string> GetCssFilePath(this Site site, IFileSystemManager fileSystemManager)
  {
    if (site.SiteSettings.TryGetValue(Site.SiteFilesKeys.CSSFILE_FILEID, out Guid fileId))
    {
      if (fileId == Guid.Empty)
      {
        return "";
      }
      else
      {
        return await GetDirectFilePath(site, fileId, fileSystemManager);
      }
    }
    return null;
  }

  /// <summary>
  /// Return a direct file path, if the file system provider supports it.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="fileId"></param>
  /// <param name="fileSystemManager"></param>
  /// <returns></returns>
  /// <remarks>
  /// Return a direct link if the file system provider supports it (because it is faster than returning a redirect to Azure Blob Storage).  This "skips"
  /// the Nucleus permissions check, but the performance difference is > 200ms.  This function should only be used for cases where it is ok to
  /// skip the permission check, like site logo/css/favicon.
  /// 
  /// If the file system provider does not support a direct link, a link to the FileController with an encoded file id parameter
  /// is returned, with a ~/ prefix.  Callers must call IUrlHelper.Content on the result of this function.
  /// </remarks>
  private async static Task<string> GetDirectFilePath(this Site site, Guid fileId, IFileSystemManager fileSystemManager)
  {
    File file = await fileSystemManager.GetFile(site, fileId);
    // render a direct link if the file system provider supports it (because it is faster than returning a redirect to remote storage).  This "skips"
    // the Nucleus permissions check, but the performance difference is > 200ms.
    if (file.Capabilities.CanDirectLink)
    {
      System.Uri uri = await fileSystemManager.GetFileDirectUrl(site, file);
      if (uri != null)
      {
        return uri.AbsoluteUri + $"?d={file.DateModified.ToBinary()}";
      }
      else
      {
        return "";
      }
    }
    else
    {
      return $"~/{Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH_PREFIX}/{FileExtensions.EncodeFileId(file.Id)}?d={file.DateModified.ToBinary()}";
    }
  }

  /// <summary>
  /// Return the absolute Uri for the specified site as a string.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="useSSL"></param>
  /// <returns></returns>
  public static string AbsoluteUrl(this Site site, Boolean useSSL)
  {
    return AbsoluteUri(site, useSSL).ToString();
  }

  /// <summary>
  /// Return the absolute Uri for the specified site.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="useSSL"></param>
  /// <returns></returns>
  public static System.Uri AbsoluteUri(this Site site, Boolean useSSL)
  {
    return new($"http{(useSSL ? "s" : "")}://{site.DefaultSiteAlias.Alias}/");
  }

  /// <summary>
  /// Return the absolute Uri for the specified site and page as a string.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  /// <param name="useSSL"></param>
  /// <returns></returns>
  public static string AbsoluteUrl(this Site site, Page page, Boolean useSSL)
  {
    return AbsoluteUri(site, page, useSSL).ToString();
  }

  /// <summary>
  /// Return the absolute Uri for the specified site and page.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  /// <param name="useSSL"></param>
  /// <returns></returns>
  public static System.Uri AbsoluteUri(this Site site, Page page, Boolean useSSL)
  {
    return new System.Uri(AbsoluteUri(site, useSSL), Normalize(page.DefaultPageRoute().Path));
  }

  private static string Normalize(string relativeUrl)
  {
    if (!string.IsNullOrEmpty(relativeUrl))
    {
      if (!relativeUrl.EndsWith('/') && !relativeUrl.Contains('#'))
      {
        relativeUrl += "/";
      }
    }

    //if (!string.IsNullOrEmpty(relativeUrl))
    //{
    //  return relativeUrl.Trim('/');
    //  //if (relativeUrl.StartsWith('/') && relativeUrl.Length > 1)
    //  //{
    //  //	return relativeUrl[1..];
    //  //}
    //}
    return relativeUrl;
  }

  /// <summary>
  /// Return the absolute Uri for the specified site and relative url.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="relativeUrl"></param>
  /// <param name="useSSL"></param>
  /// <returns></returns>
  public static System.Uri AbsoluteUri(this Site site, string relativeUrl, Boolean useSSL)
  {    
    return new System.Uri(AbsoluteUri(site, useSSL), Normalize(relativeUrl));
  }

  /// <summary>
  /// Return the absolute Uri for the specified site and relative url as a string.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="relativeUrl"></param>
  /// <param name="useSSL"></param>
  /// <returns></returns>
  public static string AbsoluteUrl(this Site site, string relativeUrl, Boolean useSSL)
  {
    return AbsoluteUri(site, relativeUrl, useSSL).ToString();
  }

  /// <summary>
  /// Return the absolute Uri for the specified site and page and relative url as a string.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  /// <param name="relativeUrl"></param>
  /// <param name="useSSL"></param>
  /// <returns></returns>
  public static string AbsoluteUrl(this Site site, Page page, string relativeUrl, Boolean useSSL)
  {
    return AbsoluteUri(site, page, relativeUrl, useSSL).ToString();
  }


  /// <summary>
  /// Return the absolute Uri for the specified site and page and relative url.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  /// <param name="relativeUrl"></param>
  /// <param name="useSSL"></param>
  /// <returns></returns>
  public static System.Uri AbsoluteUri(this Site site, Page page, string relativeUrl, Boolean useSSL)
  {
    return new System.Uri(AbsoluteUri(site, page, useSSL), Normalize(relativeUrl));
  }

  /// <summary>
  /// Validate the site home directory.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="key"></param>
  /// <returns></returns>
  /// <remarks>
  /// The home directory is validated using using Azure/S3 rules for container names, since that's mostly what the site home directory 
  /// will be.  This prevents some valid folder names for local storage from being used, but the more strict validation will allow users 
  /// to migrate to Azure/S3 later if required, and still have a valid home directory setting.
  /// </remarks>
  public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ValidateHomeDirectory(this Site site, string key)
  {
    Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = new();

    if (!System.Text.RegularExpressions.Regex.IsMatch(site.HomeDirectory, "^[0-9a-z]{1}[0-9a-z-]{2,61}[0-9a-z]$"))
    {
      modelState.AddModelError(key, "The site home directory must start and end with a letter or number, contain only letters, numbers and dashes, and must be lower case.");
    }

    if (!System.Text.RegularExpressions.Regex.IsMatch(site.HomeDirectory, "^(?!.*--)"))
    {
      modelState.AddModelError(key, "The site home directory must must not contain consecutive dashes.");
    }

    return modelState;
  }
}
