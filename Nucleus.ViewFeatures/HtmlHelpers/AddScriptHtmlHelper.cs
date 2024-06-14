using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.ClientResources;

namespace Nucleus.ViewFeatures.HtmlHelpers;

/// <summary>
/// Html helper used to add scripts.
/// </summary>
public static class AddScriptHtmlHelper
{
  /// <summary>
  /// Constant (enum) values used for the (this IHtmlHelper htmlHelper, WellKnownScripts script) overload.
  /// </summary>
  public enum WellKnownScripts
  {
    /// <summary>
    /// Bootstrap
    /// </summary>
    BOOTSTRAP = 10,

    /// <summary>
    /// jQuery
    /// </summary>
    JQUERY = 20,

    /// <summary>
    /// Nucleus common (shared) client-side script.
    /// </summary>
    NUCLEUS_SHARED = 30,

    /// <summary>
    /// Nucleus pagelist script (used by admin pages).
    /// </summary>
    NUCLEUS_PAGELIST = 40,

    /// <summary>
    /// Nucleus admin pages script.
    /// </summary>
    NUCLEUS_ADMIN = 50,

    /// <summary>
    /// Nucleus toggleswitch control script.
    /// </summary>
    NUCLEUS_TOGGLESWITCH = 60,

    /// <summary>
    /// Nucleus inline editing controls script.
    /// </summary>
    NUCLEUS_EDITMODE = 70,

    /// <summary>
    /// Monaco editor
    /// </summary>
    NUCLEUS_MONACO_EDITOR = 80,

    /// <summary>
    /// Blazor server (blazor.server.js)
    /// </summary>
    BLAZOR_SERVER = 90,

    /// <summary>
    /// Blazor server (blazor.web.js)
    /// </summary>
    BLAZOR_WEB = 100,

    /// <summary>
    /// Blazor WebAssembly (blazor.webassembly.js)
    /// </summary>
    BLAZOR_WEBASSEMBLY = 110
  }

  /// <summary>
  /// Constant values used to set script ordering in the AddScript overloads that have an order parameter.  Callers to AddScript can 
  /// also use an integer value, these values are provided for convenience.  Scripts added using 
  /// <see cref="AddScript(HttpContext, string, bool, bool, int, string)"/> can have the same value in the order parameter as another script, in this
  /// case they are ordered by which order AddScript was called in.
  /// </summary>
  public class WellKnownScriptOrders
  {
    /// <summary>
    /// Place the script first/early in the script order. 
    /// </summary>
    public const int EARLY = -1000;

    /// <summary>
    /// Default sort order for well-known scripts.
    /// </summary>
    public const int WELLKNOWN = 0;

    /// <summary>
    /// Default.
    /// </summary>
    public const int DEFAULT = 100;

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
    switch (script)
    {
      case WellKnownScripts.BOOTSTRAP:
        AddWellKnownScript(htmlHelper, "~/Resources/Libraries/Bootstrap/5.3.2/js/bootstrap.bundle.js");
        break;
      case WellKnownScripts.JQUERY:
        AddWellKnownScript(htmlHelper, "~/Resources/Libraries/jQuery/3.7.1/jquery.js");
        break;
      case WellKnownScripts.NUCLEUS_SHARED:
        AddWellKnownScript(htmlHelper, "~/Resources/js/nucleus-shared.js");
        break;
      case WellKnownScripts.NUCLEUS_PAGELIST:
        AddWellKnownScript(htmlHelper, "~/Resources/js/jquery-pagelist.js", true);
        break;
      case WellKnownScripts.NUCLEUS_ADMIN:
        AddWellKnownScript(htmlHelper, "~/Resources/js/nucleus-admin.js");
        break;
      case WellKnownScripts.NUCLEUS_TOGGLESWITCH:
        AddWellKnownScript(htmlHelper, "~/Resources/js/jquery-toggleswitch.js");
        break;
      case WellKnownScripts.NUCLEUS_EDITMODE:
        AddWellKnownScript(htmlHelper, "~/Resources/js/nucleus-editmode.js");
        break;
      case WellKnownScripts.NUCLEUS_MONACO_EDITOR:
        AddWellKnownScript(htmlHelper, "~/Resources/Libraries/Monaco/Nucleus/monaco-editor.js", false);
        AddWellKnownScript(htmlHelper, "~/Resources/Libraries/Monaco/0.44.0/min/vs/loader.js", false);
        AddWellKnownScript(htmlHelper, "~/Resources/Libraries/Monaco/0.44.0/min/vs/editor/editor.main.nls.js", true);
        AddWellKnownScript(htmlHelper, "~/Resources/Libraries/Monaco/0.44.0/min/vs/editor/editor.main.js", true);
        break;
      case WellKnownScripts.BLAZOR_SERVER:
        AddWellKnownScript(htmlHelper, "~/_framework/blazor.server.js");
        break;
      case WellKnownScripts.BLAZOR_WEB:
        AddWellKnownScript(htmlHelper, "~/_framework/blazor.web.js");
        break;
      case WellKnownScripts.BLAZOR_WEBASSEMBLY:
        AddWellKnownScript(htmlHelper, "~/_framework/blazor.webassembly.js");
        break;
    }

    return null;
  }

  /// <summary>
  /// Register the specified script with defaults for well-known scripts, and without the async attribute.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="script"></param>
  private static IHtmlContent AddWellKnownScript(IHtmlHelper htmlHelper, string script)
  {
    return AddWellKnownScript(htmlHelper, script, false);
  }

  /// <summary>
  /// Register the specified script with defaults for well-known scripts, and with the async attribute as specified by <paramref name="isAsync"/>.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="script"></param>
  /// <param name="isAsync"></param>
  private static IHtmlContent AddWellKnownScript(IHtmlHelper htmlHelper, string script, Boolean isAsync)
  {
    return AddScript(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(htmlHelper.ResolveExtensionUrl(script)), isAsync, false, WellKnownScriptOrders.WELLKNOWN, GetCoreVersion());
  }

  /// <summary>
  /// Register the specified script to be added to the Layout or module's scripts.  Use ~! for the currently executing view path, or ~# for the 
  /// currently executing extension.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="scriptPath"></param>
  /// <returns></returns>
  /// <remarks>
  /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
  /// tilde (~) character to specify an app-relative path, which should include the extensions folder and your
  /// extension folder name, or the scriptPath can contain the ~! for the currently executing view path, or ~# for the 
  /// currently executing extension. 
  /// </remarks>
  /// <example>
  /// @Html.AddScript("~/Extensions/MyModule/MyModule.js")
  /// </example>
  public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, string scriptPath)
  {
    return AddScript(htmlHelper, scriptPath, false);
  }

  /// <summary>
  /// Register the specified script to be added to the Layout or module's scripts.  Use ~! for the currently executing view path, or ~# for the 
  /// currently executing extension.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="scriptPath"></param>
  /// <param name="isAsync"></param>
  /// <returns></returns>
  /// <remarks>
  /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
  /// tilde (~) character to specify an app-relative path, which should include the extensions folder and your
  /// extension folder name, or the scriptPath can contain the ~! for the currently executing view path, or ~# for the 
  /// currently executing extension. 
  /// </remarks>
  /// <example>
  /// @Html.AddScript("~/Extensions/MyModule/MyModule.js")
  /// </example>
  public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, string scriptPath, Boolean isAsync)
  {
    return AddScript(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(htmlHelper.ResolveExtensionUrl(scriptPath)), isAsync, false, WellKnownScriptOrders.DEFAULT, GetCallerVersion(htmlHelper));
  }

  /// <summary>
  /// Register the specified script to be added to the Layout or module's scripts.  Use ~! for the currently executing view path, or ~# for the 
  /// currently executing extension.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="scriptPath"></param>
  /// <param name="isAsync"></param>
  /// <param name="order"></param>
  /// <returns></returns>
  /// <remarks>
  /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
  /// tilde (~) character to specify an app-relative path, which should include the extensions folder and your
  /// extension folder name, or the scriptPath can contain the ~! for the currently executing view path, or ~# for the 
  /// currently executing extension. 
  /// </remarks>
  /// <example>
  /// @Html.AddScript("~/Extensions/MyModule/MyModule.js")
  /// </example>
  public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, string scriptPath, Boolean isAsync, int order)
  {
    return AddScript(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(htmlHelper.ResolveExtensionUrl(scriptPath)), isAsync, false, order, GetCallerVersion(htmlHelper));
  }

  /// <summary>
  /// Register the specified script to be added to the Layout or module's scripts.  Use ~! for the currently executing view path, or ~# for the 
  /// currently executing extension.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="scriptPath"></param>
  /// <param name="isAsync"></param>
  /// <param name="isDynamic"></param>
  /// <returns></returns>
  /// <remarks>
  /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
  /// tilde (~) character to specify an app-relative path, which should include the extensions folder and your
  /// extension folder name, or the scriptPath can contain the ~! for the currently executing view path, or ~# for the 
  /// currently executing extension. 
  /// </remarks>
  /// <example>
  /// @Html.AddScript("~/Extensions/MyModule/MyModule.js")
  /// </example>
  public static IHtmlContent AddScript(this IHtmlHelper htmlHelper, string scriptPath, Boolean isAsync, Boolean isDynamic)
  {
    return AddScript(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(htmlHelper.ResolveExtensionUrl(scriptPath)), isAsync, isDynamic, WellKnownScriptOrders.DEFAULT, GetCallerVersion(htmlHelper));
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
    return AddScript(context, scriptPath, isAsync, !scriptPath.EndsWith(".js"), order, null);
  }

  /// <summary>
  /// Return the version of one of the Nucleus core assemblies.
  /// </summary>
  /// <remarks>
  /// Well-known scripts are hosted by Nucleus.Web, so their version should come from one of the Nucleus core assemblies.
  /// </remarks>
  /// <returns></returns>
  private static string GetCoreVersion()
  {
    return typeof(AddScriptHtmlHelper).Assembly.GetName().Version.ToString();
  }

  /// <summary>
  /// Return the version of the assembly which contains the calling controller action.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <returns></returns>
  private static string GetCallerVersion(IHtmlHelper htmlHelper)
  {
    return ((ControllerActionDescriptor)htmlHelper.ViewContext.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version.ToString();
  }

  /// <summary>
  /// Register the specified script to be added to the Layout or module's scripts.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="scriptPath"></param>
  /// <param name="isAsync"></param>
  /// <param name="isDynamic"></param>
  /// <param name="order"></param>
  /// <param name="version"></param>
  internal static IHtmlContent AddScript(HttpContext context, string scriptPath, Boolean isAsync, Boolean isDynamic, int order, string version)
  {
    ResourceFileOptions resourceFileOptions = context.RequestServices.GetService<IOptions<ResourceFileOptions>>().Value;
    //Dictionary<string, Nucleus.Abstractions.Models.StaticResources.Script> scripts = (Dictionary<string, Nucleus.Abstractions.Models.StaticResources.Script>)context.Items[SCRIPTS_ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
    ClientResources pageResources = (ClientResources)context.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] ?? new();

    if (!pageResources.Scripts.ContainsKey(scriptPath))
    {
      string finalScriptPath = scriptPath;

      if (resourceFileOptions.UseMinifiedJs && scriptPath.StartsWith("/") && !scriptPath.EndsWith(".min.js"))
      {
        Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostingEnvironment = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        string minifiedFileName = System.IO.Path.GetFileNameWithoutExtension(finalScriptPath) + ".min" + System.IO.Path.GetExtension(finalScriptPath);

        if (AddStyleHtmlHelper.LocalFileExists(webHostingEnvironment, context.Request.PathBase, scriptPath, minifiedFileName))
        {
          finalScriptPath = scriptPath.Substring(0, scriptPath.Length - System.IO.Path.GetFileName(scriptPath).Length) + minifiedFileName;
        }
      }

      pageResources.Scripts.Add(scriptPath, new Nucleus.Abstractions.Models.ClientResources.Script()
      {
        Path = finalScriptPath,
        IsAsync = isAsync,
        IsDynamic = isDynamic,
        Order = order,
        Version = version,
        IsExtensionScript = scriptPath.StartsWith("/" + Nucleus.Abstractions.RoutingConstants.EXTENSIONS_ROUTE_PATH_PREFIX, StringComparison.OrdinalIgnoreCase)
      });

      context.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] = pageResources;
    }

    return new HtmlContentBuilder();
  }


  /// <summary>
  /// Adds the scripts submitted by AddScript to the layout.  This method is intended for use by the Nucleus Core layout.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <returns></returns>
  public static IHtmlContent RenderScripts(this IHtmlHelper htmlHelper)
  {
    HtmlContentBuilder scriptOutput = new();

    //Dictionary<string, Nucleus.Abstractions.Models.StaticResources.Script> scripts = (Dictionary<string, Nucleus.Abstractions.Models.StaticResources.Script>)htmlHelper.ViewContext.HttpContext.Items[Nucleus.Extensions.StaticResources.SCRIPTS_ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
    ClientResources pageResources = (ClientResources)htmlHelper.ViewContext.HttpContext.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] ?? new();

    if (pageResources.Scripts.Any())
    {
      // Sort to ensure that a specific order is most important, followed by whether the script belongs to an extension.  The sort by IsExtensionScript is
      // so that extension scripts are rendered after core scripts.
      foreach (KeyValuePair<string, Nucleus.Abstractions.Models.ClientResources.Script> script in pageResources.Scripts.OrderBy(script => script.Value.Order).ThenBy(script => script.Value.IsExtensionScript))
      {
        if (!String.IsNullOrEmpty(script.Key))
        {
          TagBuilder builder = new("script");
          if (script.Value.IsDynamic)
          {
            builder.Attributes.Add("data-dynamic", "true");
          }
          builder.Attributes.Add("src", script.Value.Path + (!String.IsNullOrEmpty(script.Value.Version) ? (script.Value.Path.Contains('?') ? "&" : "?") + "v=" + script.Value.Version : ""));
          if (script.Value.IsAsync)
          {
            builder.Attributes.Add("async", "");
          }
          scriptOutput.AppendHtml(builder);
        }
      }

      // Once consumed, clear the scripts item to prevent double-rendering in case RenderScripts is called twice.
      pageResources.Scripts.Clear();
      htmlHelper.ViewContext.HttpContext.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] = pageResources;
    }

    return scriptOutput;
  }

}