using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Layout;
using Nucleus.Abstractions.Models;

namespace Nucleus.ViewFeatures.HtmlHelpers;

/// <summary>
/// Html helper used to render the modules which are within the specified pane for the page being rendered.
/// </summary>
/// <remarks>
/// The RenderPageContentHtmlHelper is used by Layouts.
/// </remarks>
public static class RenderPageContentHtmlHelper
{
  /// <summary>
  /// Renders the modules which are within the specified pane for the page being rendered.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="paneName"></param>
  /// <returns></returns>
  /// <remarks>
  /// Use this Html Helper in custom Layouts to render a Layout pane.
  /// </remarks>
  /// <example>
  /// @Html.RenderPaneAsync("ContentPane")
  /// </example>
  public static async Task<IHtmlContent> RenderPaneAsync(this IHtmlHelper htmlHelper, string paneName)
  {
    IModuleContentRenderer renderer = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IModuleContentRenderer>();
    Context context = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<Context>();

    return await renderer.RenderPaneAsync(htmlHelper.ViewContext, context, paneName);
  }

  /// <summary>
  /// Render a module editor for the specified <paramref name="moduleInfo" />.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="moduleInfo">The <see cref="PageModule"/> to render the editor for.</param>
  /// <returns></returns>
  /// <remarks>
  /// This overload renders a container around the module content.
  /// </remarks>
  public static async Task<IHtmlContent> RenderEditorAsync(this IHtmlHelper htmlHelper, PageModule moduleInfo)
  {
    return await RenderEditorAsync(htmlHelper, moduleInfo, true);
  }

  /// <summary>
  /// Render a module editor for the specified <paramref name="moduleInfo" />.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="moduleInfo">The <see cref="PageModule"/> to render the editor for.</param>
  /// <param name="renderContainer">Specifies whether to render a container around the module content.</param>
  /// <returns></returns>
  public static async Task<IHtmlContent> RenderEditorAsync(this IHtmlHelper htmlHelper, PageModule moduleInfo, Boolean renderContainer)
  {
    IModuleContentRenderer renderer = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IModuleContentRenderer>();
    Context context = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<Context>();

    return await renderer.RenderModuleEditor(htmlHelper.ViewContext, context.Site, context.Page, moduleInfo, context.LocalPath, renderContainer);
  }

  /// <summary>
  /// Render a control panel extension for the specified <paramref name="controlPanelExtension" />.
  /// </summary>
  /// <param name="htmlHelper"></param>
  /// <param name="controlPanelExtension">The <see cref="ControlPanelExtensionDefinition"/> to render.</param>
  /// <returns></returns>
  public static async Task<IHtmlContent> RenderControlPanelExtensionAsync(this IHtmlHelper htmlHelper, ControlPanelExtensionDefinition controlPanelExtension)
  {
    IModuleContentRenderer renderer = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IModuleContentRenderer>();
    Context context = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<Context>();

    return await renderer.RenderControlPanelExtension(htmlHelper.ViewContext, context.Site, controlPanelExtension);
  }

}
