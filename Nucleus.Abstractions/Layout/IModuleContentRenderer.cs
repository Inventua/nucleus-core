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
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Abstractions.Layout
{
	/// <summary>
	/// Defines the interface for the module content renderer.
	/// </summary>
	/// <remarks>
	/// A module content renderer renders the modules that make up a "pane" in a page.
	/// </remarks>
  /// <internal></internal>
	public interface IModuleContentRenderer
	{
		/// <summary>
		/// Render the module instances within the specified pane.
		/// </summary>
		/// <param name="htmlHelper"></param>
    /// <param name="context"></param>
		/// <param name="paneName"></param>
		/// <returns></returns>
		public Task<IHtmlContent> RenderPaneAsync(IHtmlHelper htmlHelper, Context context, string paneName);

    /// <summary>
    /// Render a module's "View" action.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="site"></param>
    /// <param name="page"></param>
    /// <param name="moduleInfo">Specifies the module being rendered.</param>
    /// <param name="localPath"></param>
    /// <param name="renderContainer">Specifies whether to wrap the module output in a container.</param>
    /// <returns></returns>

    public Task<IHtmlContent> RenderModuleView(IHtmlHelper htmlHelper, Site site, Page page, PageModule moduleInfo, LocalPath localPath, Boolean renderContainer);

		/// <summary>
		/// Render a module's "Edit" action.
		/// </summary>
		/// <param name="htmlHelper"></param>
    /// <param name="site"></param>
    /// <param name="page"></param>
		/// <param name="moduleInfo">Specifies the module being rendered.</param>
    /// <param name="localPath"></param>
		/// <param name="renderContainer">Specifies whether to wrap the module output in a container.</param>
		/// <returns></returns>
		public Task<IHtmlContent> RenderModuleEditor(IHtmlHelper htmlHelper, Site site, Page page, PageModule moduleInfo, LocalPath localPath, Boolean renderContainer);

		
	}
}
