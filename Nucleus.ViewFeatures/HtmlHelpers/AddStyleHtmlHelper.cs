using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.ClientResources;
using Nucleus.Extensions;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
  /// <summary>
  /// Html helper used to add styles.
  /// </summary>
  public static class AddStyleHtmlHelper
  {
    
    /// <summary>
    /// Constant (enum) values used for the (this IHtmlHelper htmlHelper, WellKnownScripts script) overload.
    /// </summary>
    public enum WellKnownScripts
    {
      /// <summary>
      /// Bootstrap
      /// </summary>
      BOOTSTRAP=10,

      /// <summary>
      /// Shared CSS used by all of Nucleus
      /// </summary>
      NUCLEUS_SHARED=20,

      /// <summary>
      /// CSS classes for the admin user interface.
      /// </summary>
			NUCLEUS_ADMIN=30,

      /// <summary>
      /// CSS classes used by the admin user interface and by modules which display forms.
      /// </summary>
			NUCLEUS_FORMS=40,

      /// <summary>
      /// CSS classes used by inline editing controls.  This stylesheet is added automatically when the user selects editing mode.
      /// </summary>
      NUCLEUS_EDITMODE=50,

      /// <summary>
      /// CSS classes for the Nucleus paging control.
      /// </summary>
      NUCLEUS_PAGINGCONTROL=60,

      /// <summary>
      /// CSS classes for the Monaco editor
      /// </summary>
      NUCLEUS_MONACO_EDITOR=70,

      /// <summary>
      /// Customizable CSS properties and implementation for containers ("container styles")
      /// </summary>
      NUCLEUS_CONTAINER_STYLES=80
    }

    private const string WELLKNOWN_BOOTSTRAP = "~/Resources/Libraries/Bootstrap/5.3.2/css/bootstrap.css";
    private const string WELLKNOWN_NUCLEUS_SHARED = "~/Resources/css/shared.css";
    private const string WELLKNOWN_NUCLEUS_ADMIN = "~/Resources/css/admin.css";
    private const string WELLKNOWN_NUCLEUS_FORMS = "~/Resources/css/forms.css";
    private const string WELLKNOWN_NUCLEUS_EDITMODE = "~/Resources/css/editmode.css";
    private const string WELLKNOWN_NUCLEUS_PAGINGCONTROL = "~/Shared/Controls/Views/PagingControl.css";
    private const string WELLKNOWN_NUCLEUS_MONACO_EDITOR = "~/Resources/Libraries/Monaco/0.44.0/min/vs/editor/editor.main.css";
    private const string WELLKNOWN_NUCLEUS_CONTAINER_STYLES = "~/Shared/Containers/container-styles.css";

    private static readonly string[] WELL_KNOWN_PATHS = 
    {
      WELLKNOWN_BOOTSTRAP,
      WELLKNOWN_NUCLEUS_SHARED,
      WELLKNOWN_NUCLEUS_ADMIN,
      WELLKNOWN_NUCLEUS_FORMS,
      WELLKNOWN_NUCLEUS_EDITMODE,
      WELLKNOWN_NUCLEUS_PAGINGCONTROL,
      WELLKNOWN_NUCLEUS_MONACO_EDITOR,
      WELLKNOWN_NUCLEUS_CONTAINER_STYLES
    };

    /// <summary>
    /// Style sort orders
    /// </summary>
    public class WellKnownSortOrders
    {
      /// <summary>
      /// Base sort ondex for well-known styles.
      /// </summary>
      /// <remarks>
      /// Well-known styles are added first in Html output, so that their values can be overridden by other css files.
      /// </remarks>
      public const int WELL_KNOWN_STYLES_SORT_INDEX = 0;

      /// <summary>
      /// Base sort ondex for well-known styles.
      /// </summary>
      /// <remarks>
      /// The container styles file is added after other well-known styles.
      /// </remarks>
      public const int CONTAINER_STYLES_SORT_INDEX = 5;

      /// <summary>
      /// Base sort index for unidentified css files.
      /// </summary>
      public const int DEFAULT_SORT_INDEX = 10;

      /// <summary>
      /// Base sort index for Css files which belong to an extension.
      /// </summary>
      /// <remarks>
      /// Extension styles are added after well-known styles but before site-specific Css, so that they can override Nucleus defaults, but can be 
      /// overridden by site styles.
      /// </remarks>
      public const int EXTENSION_STYLES_SORT_INDEX = 1000;

      /// <summary>
      /// Sort order for the site styles file.  
      /// </summary>
      /// <remarks>
      /// This sort order is for internal use by Nucleus.
      /// </remarks>
      public const int SITE_STYLES_SORT_INDEX = 50000;
    }

    /// <summary>
    /// Add a well-known script.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="script"></param>
    /// <returns></returns>
    public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, WellKnownScripts script)
    {
      string stylesheetPath = GetWellKnownStylePath(script);      

      if (!String.IsNullOrEmpty(stylesheetPath))
      {
        return AddStyle(htmlHelper, stylesheetPath, true, false, typeof(AddStyleHtmlHelper).Assembly.Version(), GetSortOrder(script));
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Add a well-known script.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="script"></param>
    /// <returns></returns>
    public static IHtmlContent AddStyle(ViewContext context, WellKnownScripts script)
    {
      string stylesheetPath = GetWellKnownStylePath(script);

      if (!String.IsNullOrEmpty(stylesheetPath))
      {
        return AddStyle(context, stylesheetPath, true, false, typeof(AddStyleHtmlHelper).Assembly.Version(), GetSortOrder(script));
      }
      else
      {
        return null;
      }
    }

    private static string GetWellKnownStylePath(WellKnownScripts script)
    {
      switch (script)
      {
        case WellKnownScripts.BOOTSTRAP:
          return WELLKNOWN_BOOTSTRAP;          

        case WellKnownScripts.NUCLEUS_SHARED:
          return WELLKNOWN_NUCLEUS_SHARED;          

        case WellKnownScripts.NUCLEUS_ADMIN:
          return WELLKNOWN_NUCLEUS_ADMIN;          

        case WellKnownScripts.NUCLEUS_FORMS:
          return WELLKNOWN_NUCLEUS_FORMS;          

        case WellKnownScripts.NUCLEUS_EDITMODE:
          return WELLKNOWN_NUCLEUS_EDITMODE;          

        case WellKnownScripts.NUCLEUS_PAGINGCONTROL:
          return WELLKNOWN_NUCLEUS_PAGINGCONTROL;
          
        case WellKnownScripts.NUCLEUS_MONACO_EDITOR:
          return WELLKNOWN_NUCLEUS_MONACO_EDITOR;          

        case WellKnownScripts.NUCLEUS_CONTAINER_STYLES:
          return WELLKNOWN_NUCLEUS_CONTAINER_STYLES;

        default:
          return string.Empty;
      }
    }

    /// <summary>
    /// Register the specified style to be added to the Layout or module's CSS styles.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="stylesheetPath"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain the 
    ///  ~! for the currently executing view path, or ~# for the currently executing extension. 
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.css")
    /// </example>
    public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, string stylesheetPath)
    {
      return AddStyle(htmlHelper, stylesheetPath, true);
    }

    /// <summary>
    /// Register the specified style to be added to the Layout or module's CSS styles.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="stylesheetPath"></param>
    /// <param name="sortIndex"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain 
    ///  ~! for the currently executing view path, or ~# for the currently executing extension. 
    /// </remarks>
    public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, string stylesheetPath, int sortIndex)
    {
      return AddStyle(htmlHelper, stylesheetPath, true, false, GetVersion(htmlHelper.ViewContext), sortIndex);
    }


    /// <summary>
    /// Register the specified style to be added to the Layout or module's CSS styles.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="stylesheetPath"></param>
    /// <param name="defer"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain the 
    ///  ~! for the currently executing view path, or ~# for the currently executing extension. 
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.css")
    /// </example>
    public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, string stylesheetPath, Boolean defer)
    {
      return AddStyle(htmlHelper, stylesheetPath, defer, false);
    }

    /// <summary>
    /// Register the specified style to be added to the Layout or module's CSS styles.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="stylesheetPath"></param>
    /// <param name="defer"></param>
    /// <param name="isDynamic"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain  
    ///  ~! for the currently executing view path, or ~# for the currently executing extension. 
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.css")
    /// </example>
    public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, string stylesheetPath, Boolean defer, Boolean isDynamic)
    {
      return AddStyle(htmlHelper, stylesheetPath, defer, isDynamic, GetVersion(htmlHelper.ViewContext), GetSortOrder(stylesheetPath));
    }

    /// <summary>
    /// Register the specified style to be added to the Layout or module's CSS styles.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="stylesheetPath"></param>
    /// <param name="defer"></param>
    /// <param name="isDynamic"></param>
    /// <param name="version"></param>
    /// <param name="sortIndex"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain the 
    ///  ~! for the currently executing view path, or ~# for the currently executing extension. 
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.css")
    /// </example>
    private static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, string stylesheetPath, Boolean defer, Boolean isDynamic, string version, int sortIndex)
    {
      return AddStyle(htmlHelper.ViewContext, stylesheetPath, defer, isDynamic, version, sortIndex);
    }

    /// <summary>
    /// Register the specified style to be added to the Layout or module's CSS styles.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="stylesheetPath"></param>
    /// <param name="defer"></param>
    /// <param name="isDynamic"></param>
    /// <param name="version"></param>
    /// <param name="sortIndex"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain the 
    ///  ~! for the currently executing view path, or ~# for the currently executing extension. 
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.css")
    /// </example>
    internal static IHtmlContent AddStyle(ViewContext context, string stylesheetPath, Boolean defer, Boolean isDynamic, string version, int sortIndex)
    {
      ResourceFileOptions resourceFileOptions = context.HttpContext.RequestServices.GetService<IOptions<ResourceFileOptions>>().Value;
      //Dictionary<string, Nucleus.Abstractions.Models.StaticResources.StyleSheet> stylesheets = (Dictionary<string, Nucleus.Abstractions.Models.StaticResources.StyleSheet>)context.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
      ClientResources pageResources = (ClientResources)context.HttpContext.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] ?? new();
      stylesheetPath = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(context).Content(context.ResolveExtensionUrl(stylesheetPath));

      if (!pageResources.StyleSheets.ContainsKey(stylesheetPath))
      {
        string finalStylesheetPath = stylesheetPath;

        if (resourceFileOptions.UseMinifiedJs && stylesheetPath.StartsWith("/") && !stylesheetPath.EndsWith(".min.css"))
        {
          Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostingEnvironment = context.HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
          string minifiedFileName = System.IO.Path.GetFileNameWithoutExtension(stylesheetPath) + ".min" + System.IO.Path.GetExtension(stylesheetPath);

          if (LocalFileExists(webHostingEnvironment, context.HttpContext.Request.PathBase, stylesheetPath, minifiedFileName))
          {
            finalStylesheetPath = stylesheetPath.Substring(0, stylesheetPath.Length - System.IO.Path.GetFileName(stylesheetPath).Length) + minifiedFileName;
          }
        }

        pageResources.StyleSheets.Add(stylesheetPath, new Nucleus.Abstractions.Models.ClientResources.StyleSheet()
        {
          Path = finalStylesheetPath,
          Defer = defer,
          IsDynamic = isDynamic,
          Version = version,
          SortIndex = sortIndex
        });

        context.HttpContext.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] = pageResources;
      }

      return new HtmlContentBuilder();
    }


    private static int GetSortOrder(WellKnownScripts script)
    {
      if (script == WellKnownScripts.NUCLEUS_CONTAINER_STYLES)
      {
        return WellKnownSortOrders.CONTAINER_STYLES_SORT_INDEX;
      }
      else
      {
        return WellKnownSortOrders.WELL_KNOWN_STYLES_SORT_INDEX;
      }
    }

    private static int GetSortOrder(string path)
    {
      if (WELL_KNOWN_PATHS.Contains(path))
      {
        return WellKnownSortOrders.WELL_KNOWN_STYLES_SORT_INDEX;
      }
      else
      {
        if (path.StartsWith("~!") || path.StartsWith("~#"))
        {
          return WellKnownSortOrders.EXTENSION_STYLES_SORT_INDEX;
        }
      }

      return WellKnownSortOrders.DEFAULT_SORT_INDEX; 
    }


    private static string GetVersion(ViewContext context)
    {
      return ((ControllerActionDescriptor)context.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version.ToString();
    }

    internal static Boolean LocalFileExists(IWebHostEnvironment webHostingEnvironment, PathString pathBase, string stylesheetPath, string fileName)
    {
      string localFilePath;

      if (pathBase.HasValue && stylesheetPath.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
      {
        localFilePath = System.IO.Path.GetDirectoryName(stylesheetPath.Substring(pathBase.Value.Length).Replace('\\', '/'));
      }
      else
      {
        localFilePath = System.IO.Path.GetDirectoryName(stylesheetPath.Replace( '\\', '/'));
      }

      IFileInfo file = webHostingEnvironment.ContentRootFileProvider.GetFileInfo(localFilePath + '/' + fileName);
      return file.Exists;

      //return System.IO.File.Exists(System.IO.Path.Join(contentRootPath, localStyleFilePath, fileName));
    }

    /// <summary>
    /// Adds the scripts submitted by AddStyle to the layout.  This method is intended for use by the Nucleus Core layout.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <returns></returns>
    public static IHtmlContent RenderStyles(this IHtmlHelper htmlHelper)
    {
      HtmlContentBuilder scriptOutput = new();
      //Dictionary<string, Nucleus.Abstractions.Models.StaticResources.StyleSheet> stylesheets = (Dictionary<string, Nucleus.Abstractions.Models.StaticResources.StyleSheet>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
      ClientResources pageResources = (ClientResources)htmlHelper.ViewContext.HttpContext.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] ?? new();
     
      if (pageResources.StyleSheets.Any())
      {
        foreach (KeyValuePair<string, Nucleus.Abstractions.Models.ClientResources.StyleSheet> style in pageResources.StyleSheets.OrderBy(sheet => sheet.Value.SortIndex))
        {
          if (!String.IsNullOrEmpty(style.Key))
          {
            TagBuilder builder = new("link");
            builder.Attributes.Add("rel", "stylesheet");
            if (style.Value.IsDynamic)
            {
              builder.Attributes.Add("data-dynamic", "true");
            }

            builder.Attributes.Add("href", style.Value.Path + (!String.IsNullOrEmpty(style.Value.Version) ? (style.Value.Path.Contains('?') ? "&" : "?") + "v=" + style.Value.Version : ""));

            if (style.Value.Defer)
            {
              builder.Attributes.Add("defer", "");
            }

            scriptOutput.AppendHtml(builder);
          }
        }

        // Once consumed, clear the stylesheets item to prevent double-rendering in case RenderStyles is called twice.
        pageResources.StyleSheets.Clear();
        htmlHelper.ViewContext.HttpContext.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] = pageResources;
      }

      return scriptOutput;
    }
  }
}