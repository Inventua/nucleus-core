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
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
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
		/// Render a module editor for the module specified by moduleInfo.
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
		/// Render a module editor for the module specified by moduleInfo.
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
  }
}
