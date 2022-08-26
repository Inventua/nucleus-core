using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;

namespace Nucleus.Modules.Search
{
	/// <summary>
	/// Renders a search control.
	/// </summary>	
	/// <internal />
	/// <hidden />
	internal static class SearchRenderer
	{
		internal static async Task<IHtmlContent> Build(IHtmlHelper htmlHelper, string resultsPageUrl, string provider, Nucleus.Modules.Search.ViewModels.Settings.DisplayModes displayMode, string prompt, int maximumSuggestions, Boolean includeFiles, string includeScopes, object htmlAttributes)
		{
			ViewModels.Viewer model = new();

			if (!String.IsNullOrEmpty(resultsPageUrl) && !resultsPageUrl.StartsWith("~/"))
			{
				resultsPageUrl = "~/" + resultsPageUrl;
			}

			model.Settings.SearchProvider = provider;
			model.SearchTerm = htmlHelper.ViewContext.HttpContext.Request.Query["search"];
			model.ResultsUrl = resultsPageUrl;
			model.Settings.DisplayMode = displayMode;
			model.Settings.Prompt = prompt;
			model.Settings.MaximumSuggestions = maximumSuggestions;
			model.Settings.IncludeFiles = includeFiles;
			model.Settings.IncludeScopes = includeScopes;

			// This (generally) gets called by a tag helper or Html helper in a layout, so the "current" folder is the layout's folder - so we 
			// must specify a full path to the view.
 			return await Microsoft.AspNetCore.Mvc.Rendering.HtmlHelperPartialExtensions.PartialAsync(htmlHelper, "/Extensions/Search/Views/Viewer.cshtml", model);
		}		
	}
}
