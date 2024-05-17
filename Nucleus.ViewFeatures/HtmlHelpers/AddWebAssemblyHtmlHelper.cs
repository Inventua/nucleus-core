using System;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.ClientResources;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.ViewFeatures.HtmlHelpers;

/// <summary>
/// Html helper used to register Web Assemblies for dynamically loading.
/// </summary>
public static class AddWebAssemblyHtmlHelper
{
  /// <summary>
  /// Register the specified WebAssembly to be dynamically loaded.  Use the ~! prefix for the currently executing view path, or ~# for the 
  /// currently executing extension.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="assemblyPath"></param>
  /// <returns></returns>
  /// <remarks>
  /// Extensions (modules) can use this Html Helper to add scripts to the HEAD block.  The scriptPath can contain the 
  /// tilde (~) character to specify an app-relative path, which should include the extensions folder and your
  /// extension folder name, or the scriptPath can contain the ~! for the currently executing view path, or ~# for the 
  /// currently executing extension. 
  /// </remarks>
  /// <example>
  /// @Html.AddWebAssembly("~#/bin/MyModule.wasm")
  /// </example>
  public static IHtmlContent AddWebAssembly(this IHtmlHelper htmlHelper, string assemblyPath)
  {
    // if we are adding webassemblies, then we will want the blazor runtime, so add it automatically
    htmlHelper.AddScript(AddScriptHtmlHelper.WellKnownScripts.BLAZOR_WEB);

    return AddWebAssembly(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(htmlHelper.ResolveExtensionUrl(assemblyPath)), GetCallerVersion(htmlHelper));
  }

  /// <summary>
  /// Register the specified web assembly to be dynamically loaded by the browser.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="assemblyPath"></param>
  /// <param name="version"></param>
  internal static IHtmlContent AddWebAssembly(HttpContext context, string assemblyPath, string version)
  {
    ResourceFileOptions resourceFileOptions = context.RequestServices.GetService<IOptions<ResourceFileOptions>>().Value;
    ClientResources pageResources = (ClientResources)context.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] ?? new();

    if (!pageResources.WebAssemblies.ContainsKey(assemblyPath))
    {
      string finalassemblyPath = assemblyPath;

      if (resourceFileOptions.UseMinifiedJs && assemblyPath.StartsWith("/") && !assemblyPath.EndsWith(".min.js"))
      {
        Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostingEnvironment = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        string minifiedFileName = System.IO.Path.GetFileNameWithoutExtension(finalassemblyPath) + ".min" + System.IO.Path.GetExtension(finalassemblyPath);

        if (AddStyleHtmlHelper.LocalFileExists(webHostingEnvironment, context.Request.PathBase, assemblyPath, minifiedFileName))
        {
          finalassemblyPath = assemblyPath.Substring(0, assemblyPath.Length - System.IO.Path.GetFileName(assemblyPath).Length) + minifiedFileName;
        }
      }

      pageResources.WebAssemblies.Add(assemblyPath, new WebAssembly()
      {
        Path = finalassemblyPath,
        Version = version
      });

      context.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] = pageResources;
    }

    return new HtmlContentBuilder();
  }

  /// <summary>
  /// Adds the scripts submitted by AddWebAssembly to the layout.  This method is intended for use by the Nucleus Core layout.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <returns></returns>
  public static IHtmlContent RenderWebAssemblies(this IHtmlHelper htmlHelper)
  {
    HtmlContentBuilder output = new();

    ClientResources pageResources = (ClientResources)htmlHelper.ViewContext.HttpContext.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] ?? new();

    if (pageResources.WebAssemblies.Any())
    {
      TagBuilder builder = new("script");
      string assembliesArrayItems = String.Join(',', pageResources.WebAssemblies.Values.Select(webAssembly => $"'{webAssembly.Path}?ver={webAssembly.Version}'"));
      builder.InnerHtml.AppendHtml($"Page.AddWasmRuntimeAssemblies( [{assembliesArrayItems}] );");
      output.AppendHtml(builder);

      // Once consumed, clear the webassemblies list to prevent double-rendering in case RenderWebAssemblies is called twice.
      pageResources.WebAssemblies.Clear();
      htmlHelper.ViewContext.HttpContext.Items[Nucleus.Abstractions.Models.ClientResources.ClientResources.ITEMS_KEY] = pageResources;
    }

    return output;
  }

  /// <summary>
  /// Return the version of the Nucleus core assemblies.
  /// </summary>
  /// <remarks>
  /// Well-known scripts are hosted by Nucleus.Web, so their version should come from one of the Nucleus core assemblies.
  /// </remarks>
  /// <returns></returns>
  private static string GetCoreVersion()
  {
    return typeof(AddWebAssemblyHtmlHelper).Assembly.GetName().Version.ToString();
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
}