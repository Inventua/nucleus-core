using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	/// <summary>
	/// Html helper used to add the configured scripts for a Html helper to the page output.
	/// </summary>
	public static class HtmlEditorHtmlHelper
	{
		/// <summary>
		/// Add the configured scripts for a Html helper to the page output.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <returns></returns>
		public static IHtmlContent AddHtmlEditor(this IHtmlHelper htmlHelper)
		{
			HtmlEditorOptions options = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IOptions<HtmlEditorOptions>>().Value;

			if (options.HtmlEditors.Any())
			{
				// If the cached default is set, use it
				HtmlEditorConfig defaultEditor = options.DefaultHtmlEditorConfig;

				// If the cached default is not set, determine the default editor
				if (defaultEditor == null)
				{
					// Default to the first Html editor if no default is specified
					if (String.IsNullOrEmpty(options.Default))
					{
						options.Default = options.HtmlEditors.First().Key;
					}

					defaultEditor = options.HtmlEditors.Where(editor => editor.Key.Equals(options.Default, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
					if (defaultEditor == null)
					{
						// If the default doesn't match the key of any Html editor configs, use the first one
						options.Default = options.HtmlEditors.First().Key;
						defaultEditor = options.HtmlEditors.First();
						options.SetDefaultHtmlEditorConfig(defaultEditor);
					}
				}

				// Add the specified stylesheets and javascript for the default Html editor.
				foreach (HtmlEditorScript option in defaultEditor.Scripts)
				{
					switch (option.Type)
					{
						case HtmlEditorScript.Types.javascript:
							htmlHelper.AddScript(option.Path);
							break;
						case HtmlEditorScript.Types.stylesheet:
							htmlHelper.AddStyle(option.Path);
							break;
					}
				}
			}

			return new HtmlContentBuilder();
		}
	}
}
