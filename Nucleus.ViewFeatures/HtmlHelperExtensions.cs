using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.ViewFeatures;

/// <summary>
/// Extensions which use the HtmlHelper class.
/// </summary>
/// <internal />
internal static class HtmlHelperExtensions
{
  /// <summary>
  /// Special token (~!) used by the AddScript and AddStyle html helpers to represent the path of the currently executing view.
  /// </summary>
  public static string VIEWPATH_TOKEN = "~!";

  /// <summary>
  /// Special token (~#) used by the AddScript and AddStyle html helpers to represent the extension path of the currently executing view.
  /// </summary>
  public static string EXTENSIONPATH_TOKEN = "~#";

  /// <summary>
  /// Generates an absolute url from the passed-in relative url after substituting ~! for the currently executing view path, or ~#
  /// for the currently executing extension.
  /// </summary>
  /// <param name="helper"></param>
  /// <param name="url"></param>
  /// <returns></returns>
  public static string ResolveExtensionUrl(this IHtmlHelper helper, string url)
  {
    return ResolveExtensionUrl(helper.ViewContext, url);
  }

  /// <summary>
  /// Generates an absolute url from the passed-in relative url after substituting ~! for the currently executing view path, or ~#
  /// for the currently executing extension.
  /// </summary>
  /// <param name="viewContext"></param>
  /// <param name="url"></param>
  /// <returns></returns>
  public static string ResolveExtensionUrl(this ViewContext viewContext, string url)
  {
    IUrlHelper urlHelper = viewContext.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(viewContext);

    if (url.StartsWith(VIEWPATH_TOKEN))
    {
      string executingViewPath = viewContext.ExecutingFilePath;
      System.Uri viewPath = urlHelper.GetAbsoluteUri(System.IO.Path.GetDirectoryName(executingViewPath).Replace("\\", "/"));
      return new System.Uri(viewPath, ParseScriptPath(url, VIEWPATH_TOKEN)).AbsolutePath;
    }
    else if (url.StartsWith(EXTENSIONPATH_TOKEN))
    {
      string viewPath = System.IO.Path.GetDirectoryName(viewContext.ExecutingFilePath).Replace("\\", "/");
      string[] viewPathParts = viewPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

      // viewPathParts[0] will be "extensions", and viewPathParts[1] will be the extension folder name
      if (viewPathParts.Length > 2)
      {
        System.Uri extensionPath = urlHelper.GetAbsoluteUri($"{viewPathParts[0]}/{viewPathParts[1]}");
        return new System.Uri(extensionPath, ParseScriptPath(url, EXTENSIONPATH_TOKEN)).AbsolutePath;
      }
    }

    return url;
  }

  private static string ParseScriptPath(string url, string token)
  {
    string scriptPath = url[token.Length..];
    if (scriptPath.StartsWith('/'))
    {
      scriptPath = scriptPath[1..];
    }
    return scriptPath;
  }
}
