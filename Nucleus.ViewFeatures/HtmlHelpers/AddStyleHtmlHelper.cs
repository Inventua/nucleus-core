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
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	/// <summary>
	/// Html helper used to add styles.
	/// </summary>
	public static class AddStyleHtmlHelper
	{
		private const string ITEMS_KEY = "STYLESHEETS_SECTION";

		/// <summary>
		/// Register the specified style to be added to the Layout or module's CSS styles.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="scriptPath"></param>
		/// <returns></returns>
		/// <remarks>
		/// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain the 
		///  ~! for the currently executing view path, or ~# for the currently executing extension. 
		/// </remarks>
		/// <example>
		/// @Html.AddScript("~/Extensions/MyModule/MyModule.css")
		/// </example>
		public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, string scriptPath)
		{
			Dictionary<string, System.Version> scripts = (Dictionary<string, System.Version>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);

			scriptPath = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).ResolveExtensionUrl(scriptPath);			

			if (!scripts.ContainsKey(scriptPath))
			{
				scripts.Add(scriptPath, System.Reflection.Assembly.GetCallingAssembly().GetName().Version);
				htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] = scripts;
			}

			return new HtmlContentBuilder();
		}

		/// <summary>
		/// Adds the scripts submitted by AddStyle to the layout.  This method is intended for use by the Nucleus Core layout.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <returns></returns>
		public static IHtmlContent RenderStyles(this IHtmlHelper htmlHelper)
		{
			HtmlContentBuilder scriptOutput = new();

			Dictionary<string, System.Version> styles = (Dictionary<string, System.Version>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY];
			if (styles != null)
			{
				foreach (KeyValuePair<string, System.Version> style in styles)
				{
					TagBuilder builder = new("link");
					builder.Attributes.Add("rel", "stylesheet");
					builder.Attributes.Add("href", new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(style.Key) + "?v=" + style.Value.ToString());

					scriptOutput.AppendHtml(builder);
				}
			}

			return scriptOutput;
		}
	}
}