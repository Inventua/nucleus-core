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
			return AddScript(htmlHelper.ViewContext.HttpContext, new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(htmlHelper.ViewContext).Content(scriptPath), isAsync, 0, ((ControllerActionDescriptor)htmlHelper.ViewContext.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Version);				
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
		/// tilde (~) character to specify an app-relative path.  Your script path should be absolute.  This overload does not append a version querystring element.
		/// </remarks>
		public static IHtmlContent AddScript(this HttpContext context, string scriptPath, Boolean isAsync, int order)
		{
			return AddScript(context, scriptPath, isAsync, order, null);
		}

		private static IHtmlContent AddScript(HttpContext context, string scriptPath, Boolean isAsync, int order, Version version)
		{
			ResourceFileOptions resourceFileOptions = context.RequestServices.GetService<IOptions<ResourceFileOptions>>().Value;
			Dictionary<string, ScriptInfo> scripts = (Dictionary<string, ScriptInfo>)context.Items[ITEMS_KEY] ?? new(StringComparer.OrdinalIgnoreCase);
						
			if (!scripts.ContainsKey(scriptPath))
			{
				string finalScriptPath = scriptPath;

				if (resourceFileOptions.UseMinifedJs && scriptPath.StartsWith("/") && !scriptPath.EndsWith(".min.js"))
				{
					Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostingEnvironment = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
					string minifiedPath = System.IO.Path.Join(webHostingEnvironment.ContentRootPath, System.IO.Path.GetDirectoryName(scriptPath.Replace('/', Path.DirectorySeparatorChar)), System.IO.Path.GetFileNameWithoutExtension(scriptPath)) + ".min" + System.IO.Path.GetExtension(scriptPath);
					if (System.IO.File.Exists(minifiedPath))
					{
						finalScriptPath = scriptPath.Substring(0, scriptPath.Length - System.IO.Path.GetFileName(scriptPath).Length) + System.IO.Path.GetFileNameWithoutExtension(scriptPath) + ".min" + System.IO.Path.GetExtension(scriptPath);
							// minifiedPath.Substring(webHostingEnvironment.ContentRootPath.Length);
					}
				}
				
				scripts.Add(scriptPath, new ScriptInfo() 
				{ 
					Path = finalScriptPath, 
					IsAsync = isAsync, 
					Order = order, 
					Version = version 
				});
				context.Items[ITEMS_KEY] = scripts;
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
				foreach (KeyValuePair<string, ScriptInfo> script in scripts.OrderBy(script => script.Value.Order))
				{
					if (!String.IsNullOrEmpty(script.Key))
					{
						TagBuilder builder = new("script");
						builder.Attributes.Add("type", "text/javascript");
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
			public string Path { get; set; }
			public int Order { get; set; } = 0;

		}
	}
}