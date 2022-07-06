using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
  /// <summary>
  /// Html helper used to add styles.
  /// </summary>
  public static class AddStyleHtmlHelper
  {
    private const string ITEMS_KEY = "STYLESHEETS_SECTION";

    /// <summary>
    /// Constant (enum) values used for the (this IHtmlHelper htmlHelper, WellKnownScripts script) overload.
    /// </summary>
    public enum WellKnownScripts
    {
      /// <summary>
      /// Bootstrap
      /// </summary>
      BOOTSTRAP
    }

    /// <summary>
    /// Add a well-known script.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="script"></param>
    /// <returns></returns>
    public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, WellKnownScripts script)
    {
      string stylesheetPath = "";

      switch (script)
      {
        case WellKnownScripts.BOOTSTRAP:
          stylesheetPath = "~/Resources/Libraries/Bootstrap/5.0.2/css/bootstrap.css";
          break;
      }

      if (!String.IsNullOrEmpty(stylesheetPath))
      {
        return AddStyle(htmlHelper, stylesheetPath);
      }
      else
      {
        return null;
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
    /// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain the 
    ///  ~! for the currently executing view path, or ~# for the currently executing extension. 
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.css")
    /// </example>
    public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, string stylesheetPath, Boolean defer, Boolean isDynamic)
    {
      ResourceFileOptions resourceFileOptions = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IOptions<ResourceFileOptions>>().Value;
      Dictionary<string, StylesheetInfo> stylesheets = (Dictionary<string, StylesheetInfo>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);

      stylesheetPath = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(htmlHelper.ResolveExtensionUrl(stylesheetPath));

      if (!stylesheets.ContainsKey(stylesheetPath))
      {
        string finalStylesheetPath = stylesheetPath;

        if (resourceFileOptions.UseMinifiedJs && stylesheetPath.StartsWith("/") && !stylesheetPath.EndsWith(".min.css"))
        {
          Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostingEnvironment = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
          string minifiedFileName = System.IO.Path.GetFileNameWithoutExtension(stylesheetPath) + ".min" + System.IO.Path.GetExtension(stylesheetPath);
         
          if (LocalFileExists(webHostingEnvironment.ContentRootPath, htmlHelper.ViewContext.HttpContext.Request.PathBase, stylesheetPath, minifiedFileName))
          {
            finalStylesheetPath = stylesheetPath.Substring(0, stylesheetPath.Length - System.IO.Path.GetFileName(stylesheetPath).Length) + minifiedFileName;
          }
        }

        stylesheets.Add(stylesheetPath, new StylesheetInfo()
        {
          Path = finalStylesheetPath,
          Defer = defer,
          IsDynamic = isDynamic,
          Version = ((ControllerActionDescriptor)htmlHelper.ViewContext.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version
        });

        htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] = stylesheets;
      }

      return new HtmlContentBuilder();
    }

    private static Boolean LocalFileExists(string contentRootPath, PathString pathBase, string stylesheetPath, string fileName)
    {
      string localStyleFilePath;

      if (pathBase.HasValue && stylesheetPath.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
      {
        localStyleFilePath = System.IO.Path.GetDirectoryName(stylesheetPath.Substring(pathBase.Value.Length).Replace('/', Path.DirectorySeparatorChar));
      }
      else
      {
        localStyleFilePath = System.IO.Path.GetDirectoryName(stylesheetPath.Replace('/', Path.DirectorySeparatorChar)); ;
      }

      return System.IO.File.Exists(System.IO.Path.Join(contentRootPath, localStyleFilePath, fileName));
    }

    /// <summary>
    /// Adds the scripts submitted by AddStyle to the layout.  This method is intended for use by the Nucleus Core layout.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <returns></returns>
    public static IHtmlContent RenderStyles(this IHtmlHelper htmlHelper)
    {
      HtmlContentBuilder scriptOutput = new();
      Dictionary<string, StylesheetInfo> stylesheets = (Dictionary<string, StylesheetInfo>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);

      if (stylesheets != null)
      {
        foreach (KeyValuePair<string, StylesheetInfo> style in stylesheets)
        {
          if (!String.IsNullOrEmpty(style.Key))
          {
            TagBuilder builder = new("link");
            builder.Attributes.Add("rel", "stylesheet");
            if (style.Value.IsDynamic)
            {
              builder.Attributes.Add("data-dynamic", "true");
            }

            builder.Attributes.Add("href", style.Value.Path + (style.Value.Version != null ? "?v=" + style.Value.Version.ToString() : ""));

            if (style.Value.Defer)
            {
              builder.Attributes.Add("defer", "");
            }

            scriptOutput.AppendHtml(builder);
          }
        }

        // Once consumed, clear the stylesheets item to prevent double-rendering in case RenderStyles is called twice.
        htmlHelper.ViewContext.HttpContext.Items.Remove(ITEMS_KEY);
      }

      return scriptOutput;
    }


    private class StylesheetInfo
    {
      public System.Version Version { get; set; }
      public Boolean Defer { get; set; }
      public Boolean IsDynamic { get; set; }
      public string Path { get; set; }
    }
  }
}