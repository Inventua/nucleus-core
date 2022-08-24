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
  /// Html helper used to add scripts.
  /// </summary>
  public static class AddScriptHtmlHelper
  {
    private const string ITEMS_KEY = "SCRIPT_SECTION";

    /// <summary>
    /// Constant (enum) values used for the (this IHtmlHelper htmlHelper, WellKnownScripts script) overload.
    /// </summary>
    public enum WellKnownScripts
    {
      /// <summary>
      /// Bootstrap
      /// </summary>
      BOOTSTRAP,

      /// <summary>
      /// jQuery
      /// </summary>
      JQUERY
    }

    /// <summary>
    /// Constant values used to set script ordering in the AddScript overloads that have an order parameter.  Callers to AddScript can 
    /// also use an integer value, these values are provided for convenience.  Scripts added using 
    /// <see cref="AddScript(HttpContext, string, bool, bool, int, Version)"/> can have the same value in the order parameter as another script, in theis
    /// case they are ordered by which order AddScript was called in.
    /// </summary>
    public class WellKnownScriptOrders
		{
      /// <summary>
      /// Place the script first/early in the script order. 
      /// </summary>
      public const int EARLY = -1000;

      /// <summary>
      /// Default.
      /// </summary>
      public const int DEFAULT = 0;

      /// <summary>
      /// Place the script late in the script order.
      /// </summary>
      public const int LATE = 1000;
		}

    /// <summary>
    /// Add a well-known script.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="script"></param>
    /// <returns></returns>
    public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, WellKnownScripts script)
    {
      string scriptPath = "";

      switch (script)
      {
        case WellKnownScripts.BOOTSTRAP:
          scriptPath = "~/Resources/Libraries/Bootstrap/5.0.2/js/bootstrap.bundle.js";
          break;
        case WellKnownScripts.JQUERY:
          scriptPath = "~/Resources/Libraries/jQuery/03.06.00/jquery.js";
          break;
      }

      if (!String.IsNullOrEmpty(scriptPath))
      {
        return AddScript(htmlHelper, scriptPath, false, WellKnownScriptOrders.EARLY);
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Register the specified script to be added to the Layout or module's scripts.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="scriptPath"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
    /// tilde (~) character to specify an app-relative path.  Your script path should include the extensions folder and your
    /// extension folder name.
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.js")
    /// </example>
    public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, string scriptPath)
    {
      return AddScript(htmlHelper, scriptPath, false);
    }

    /// <summary>
    /// Register the specified script to be added to the Layout or module's scripts.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="scriptPath"></param>
    /// <param name="isAsync"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
    /// tilde (~) character to specify an app-relative path.  Your script path should include the extensions folder and your
    /// extension folder name.
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.js")
    /// </example>
    public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, string scriptPath, Boolean isAsync)
    {
      return AddScript(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(htmlHelper.ResolveExtensionUrl(scriptPath)), isAsync, false, WellKnownScriptOrders.DEFAULT, ((ControllerActionDescriptor)htmlHelper.ViewContext.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version);
    }

    /// <summary>
    /// Register the specified script to be added to the Layout or module's scripts.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="scriptPath"></param>
    /// <param name="isAsync"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
    /// tilde (~) character to specify an app-relative path.  Your script path should include the extensions folder and your
    /// extension folder name.
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.js")
    /// </example>
    public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, string scriptPath, Boolean isAsync, int order)
    {
      return AddScript(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(htmlHelper.ResolveExtensionUrl(scriptPath)), isAsync, false, order, ((ControllerActionDescriptor)htmlHelper.ViewContext.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version);
    }

    /// <summary>
    /// Register the specified script to be added to the Layout or module's scripts.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="scriptPath"></param>
    /// <param name="isAsync"></param>
    /// <param name="isDynamic"></param>
    /// <returns></returns>
    /// <remarks>
    /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
    /// tilde (~) character to specify an app-relative path.  Your script path should include the extensions folder and your
    /// extension folder name.
    /// </remarks>
    /// <example>
    /// @Html.AddScript("~/Extensions/MyModule/MyModule.js")
    /// </example>
    public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, string scriptPath, Boolean isAsync, Boolean isDynamic)
    {
      return AddScript(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(scriptPath), isAsync, isDynamic, WellKnownScriptOrders.DEFAULT, ((ControllerActionDescriptor)htmlHelper.ViewContext.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version);
    }

    /// <summary>
    /// Register the specified script to be added to the Layout or module's scripts.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scriptPath"></param>
    /// <param name="isAsync"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    /// <remarks>
    /// This overload is intended for use by extensions which need to add a script from code other than a view.  This overload does not support the 
    /// tilde (~) character to specify an app-relative path.  This overload does not append a version querystring element.
    /// </remarks>
    public static IHtmlContent AddScript(this HttpContext context, string scriptPath, Boolean isAsync, int order)
    {
      return AddScript(context, scriptPath, isAsync, false, order, null);
    }

    private static IHtmlContent AddScript(HttpContext context, string scriptPath, Boolean isAsync, Boolean isDynamic, int order, Version version)
    {
      ResourceFileOptions resourceFileOptions = context.RequestServices.GetService<IOptions<ResourceFileOptions>>().Value;
      Dictionary<string, ScriptInfo> scripts = (Dictionary<string, ScriptInfo>)context.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
      
      if (!scripts.ContainsKey(scriptPath))
      {
        string finalScriptPath = scriptPath;

        if (resourceFileOptions.UseMinifiedJs && scriptPath.StartsWith("/") && !scriptPath.EndsWith(".min.js"))
        {
          Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostingEnvironment = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
          string minifiedFileName = System.IO.Path.GetFileNameWithoutExtension(finalScriptPath) + ".min" + System.IO.Path.GetExtension(finalScriptPath);

          if (LocalFileExists(webHostingEnvironment.ContentRootPath, context.Request.PathBase, scriptPath, minifiedFileName))
          {
            finalScriptPath = scriptPath.Substring(0, scriptPath.Length - System.IO.Path.GetFileName(scriptPath).Length) + minifiedFileName;
          }
        }

        scripts.Add(scriptPath, new ScriptInfo()
        {
          Path = finalScriptPath,
          IsAsync = isAsync,
          IsDynamic = isDynamic,
          Order = order,
          Version = version,
          IsExtensionScript = scriptPath.StartsWith("/" + Nucleus.Abstractions.RoutingConstants.EXTENSIONS_ROUTE_PATH, StringComparison.OrdinalIgnoreCase)
        });
        context.Items[ITEMS_KEY] = scripts;
      }

      return new HtmlContentBuilder();
    }
    private static Boolean LocalFileExists(string contentRootPath, PathString pathBase, string scriptPath, string fileName)
    {
      string localStyleFilePath;

      if (pathBase.HasValue && scriptPath.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
      {
        localStyleFilePath = System.IO.Path.GetDirectoryName(scriptPath.Substring(pathBase.Value.Length).Replace('/', Path.DirectorySeparatorChar));
      }
      else
      {
        localStyleFilePath = System.IO.Path.GetDirectoryName(scriptPath.Replace('/', Path.DirectorySeparatorChar)); ;
      }

      return System.IO.File.Exists(System.IO.Path.Join(contentRootPath, localStyleFilePath, fileName));
    }

    /// <summary>
    /// Adds the scripts submitted by AddScript to the layout.  This method is intended for use by the Nucleus Core layout.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <returns></returns>
    public static IHtmlContent RenderScripts(this IHtmlHelper htmlHelper)
    {
      HtmlContentBuilder scriptOutput = new();

      Dictionary<string, ScriptInfo> scripts = (Dictionary<string, ScriptInfo>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
      if (scripts != null)
      {
        // Sort to ensure that a specific order is most important, followed by whether the script belongs to an extension.  The sort by IsExtensionScript is
        // so that extension scripts are rendered after core scripts.
        foreach (KeyValuePair<string, ScriptInfo> script in scripts.OrderBy(script => script.Value.Order).ThenBy(script => script.Value.IsExtensionScript))
        {
          if (!String.IsNullOrEmpty(script.Key))
          {
            TagBuilder builder = new("script");
            if (script.Value.IsDynamic)
            {
              builder.Attributes.Add("data-dynamic", "true");
            }
            builder.Attributes.Add("src", script.Value.Path + (script.Value.Version != null ? "?v=" + script.Value.Version.ToString() : ""));
            if (script.Value.IsAsync)
            {
              builder.Attributes.Add("async", "");
            }
            scriptOutput.AppendHtml(builder);
          }
        }

        // Once consumed, clear the scripts item to prevent double-rendering in case RenderScripts is called twice.
        htmlHelper.ViewContext.HttpContext.Items.Remove(ITEMS_KEY);
      }

      return scriptOutput;
    }

    private class ScriptInfo
    {
      public System.Version Version { get; set; }
      public Boolean IsAsync { get; set; }
      public Boolean IsDynamic { get; set; }
      public string Path { get; set; }
      public int Order { get; set; } = WellKnownScriptOrders.DEFAULT;
      public Boolean IsExtensionScript { get; set; } = false;

    }
  }
}