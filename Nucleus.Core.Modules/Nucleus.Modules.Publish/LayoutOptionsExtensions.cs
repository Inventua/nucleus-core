using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Modules.Publish.Models;

namespace Nucleus.Modules.Publish
{
  public static class LayoutOptionsExtensions
  {
    public static string GetMasterLayoutPath(this LayoutOptions options, string viewerLayoutPath)
    {
      return $"{viewerLayoutPath}/{options.ViewerLayout}/MasterLayouts/{options.MasterLayout}.cshtml";
    }

    public static string GetPrimaryArticleLayoutPath(this LayoutOptions options, string viewerLayoutPath)
    {
      if (!String.IsNullOrEmpty(options.PrimaryArticleLayout))
      {
        return $"{viewerLayoutPath}/{options.ViewerLayout}/ArticleLayouts/{options.PrimaryArticleLayout}.cshtml";
      }

      return string.Empty;
    }

    public static string GetSecondaryArticleLayoutPath(this LayoutOptions options, string viewerLayoutPath)
    {
      if (!String.IsNullOrEmpty(options.SecondaryArticleLayout))
      {
        return $"{viewerLayoutPath}/{options.ViewerLayout}/ArticleLayouts/{options.SecondaryArticleLayout}.cshtml";
      }
      return string.Empty;
    }

    public static IEnumerable<string> ListViewerLayouts(this LayoutOptions options, string viewerLayoutFolder)
    {
      return System.IO.Directory.EnumerateDirectories($"{viewerLayoutFolder}")
        .OrderBy(foldername => foldername)
        .Select(folder => System.IO.Path.GetFileName(folder));
    }

    public static IEnumerable<string> ListMasterLayouts(this LayoutOptions options, string viewerLayoutFolder)
    {
      if (options.ViewerLayout != null && System.IO.Directory.Exists($"{viewerLayoutFolder}{options.ViewerLayout}\\MasterLayouts\\"))
      {
        return System.IO.Directory.EnumerateFiles($"{viewerLayoutFolder}{options.ViewerLayout}\\MasterLayouts\\", "*.cshtml")
          .OrderBy(layout => layout)
          .Select(file => System.IO.Path.GetFileNameWithoutExtension(file));
      }
      else
      {
        return Enumerable.Empty<string>();
      }
    }

    public static IEnumerable<string> ListArticleLayouts(this LayoutOptions options, string viewerLayoutFolder)
    {
      if (options.ViewerLayout != null && System.IO.Directory.Exists($"{viewerLayoutFolder}{options.ViewerLayout}\\ArticleLayouts\\"))
      {
        // Get the article layout files in the selected ViewerLayout folder
        return System.IO.Directory.EnumerateFiles($"{viewerLayoutFolder}{options.ViewerLayout}\\ArticleLayouts\\", "*.cshtml")
          .OrderBy(layout => layout)
          .Select(file => System.IO.Path.GetFileNameWithoutExtension(file));
      }
      else
      {
        return Enumerable.Empty<string>();
      }
    }
  }
}
