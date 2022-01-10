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
	public static class AddStyleHtmlHelper
	{
		private const string ITEMS_KEY = "STYLESHEETS_SECTION";

		/// <summary>
		/// Register the specified style to be added to the Layout's CSS styles.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="scriptPath"></param>
		/// <returns></returns>
		/// <remarks>
		/// Extensions (modules) can use this Html Helper to add CSS stylesheets to the HEAD block.  The scriptPath can contain the 
		/// tilde (~) character to specify an app-relative path.  Your script path should include the extensions folder and your
		/// extension folder name.
		/// </remarks>
		/// <example>
		/// @Html.AddScript("~/Extensions/MyModule/MyModule.css")
		/// </example>
		public static IHtmlContent AddStyle(this IHtmlHelper htmlHelper, string scriptPath)
		{
			List<string> scripts = (List<string>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new();

			scriptPath = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).ResolveExtensionUrl(scriptPath);			

			if (!scripts.Contains(scriptPath, StringComparer.OrdinalIgnoreCase))
			{
				scripts.Add(scriptPath);
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

			List<string> scripts = (List<string>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY];
			if (scripts != null)
			{
				foreach (string path in scripts)
				{
					TagBuilder builder = new TagBuilder("link");
					builder.Attributes.Add("rel", "stylesheet");
					builder.Attributes.Add("href", new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(path));

					scriptOutput.AppendHtml(builder);
				}
			}

			return scriptOutput;
		}
	}
}