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

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	/// <summary>
	/// Html helper used to add scripts.
	/// </summary>
	public static class AddScriptHtmlHelper
	{
		private const string ITEMS_KEY = "SCRIPT_SECTION";

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
			//Dictionary<string, System.Version> scripts = (Dictionary<string, System.Version>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, ScriptInfo> scripts = (Dictionary<string, ScriptInfo>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);

			scriptPath = htmlHelper.ResolveExtensionUrl(scriptPath);

			if (!scripts.ContainsKey(scriptPath))
			{
				scripts.Add(scriptPath, new ScriptInfo() { Path = scriptPath, IsAsync = isAsync, Version = ((ControllerActionDescriptor)htmlHelper.ViewContext.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version });
				htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] = scripts;
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

			Dictionary<string, ScriptInfo> scripts = (Dictionary<string, ScriptInfo>)htmlHelper.ViewContext.HttpContext.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
			if (scripts != null)
			{
				foreach (KeyValuePair<string, ScriptInfo> script in scripts)
				{
					if (!String.IsNullOrEmpty(script.Key))
					{
						TagBuilder builder = new("script");
						builder.Attributes.Add("type", "text/javascript");
						builder.Attributes.Add("src", new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(script.Key) + "?v=" + script.Value.Version.ToString());
						if (script.Value.IsAsync)
						{
							builder.Attributes.Add("async", "");
						}
						scriptOutput.AppendHtml(builder);
					}
				}
			}

			return scriptOutput;
		}

		private class ScriptInfo
		{
			public System.Version Version { get; set; }	
			public Boolean IsAsync { get; set; }
			public string Path { get; set; }
		}
	}
}