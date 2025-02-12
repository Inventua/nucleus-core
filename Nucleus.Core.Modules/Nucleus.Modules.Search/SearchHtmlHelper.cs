﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Modules.Search
{
	public static class SearchHtmlHelper
	{
		/// <summary>
		/// Renders a search control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="resultsPageUrl"></param>
		/// <param name="displayMode"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static async Task<IHtmlContent> Search(this IHtmlHelper htmlHelper, string resultsPageUrl, string provider, Nucleus.Modules.Search.ViewModels.Settings.DisplayModes displayMode, object htmlAttributes)
		{
			return await Search(htmlHelper, resultsPageUrl, provider, displayMode, Nucleus.Modules.Search.ViewModels.Settings.PROMPT_DEFAULT, 5, true, "", htmlAttributes);
		}

		/// <summary>
		/// Renders a search control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="resultsPageUrl"></param>
		/// <param name="displayMode"></param>
		/// <param name="maximumSuggestions"></param>
		/// <param name="includeFiles"></param>
		/// <param name="includeScopes"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static async Task<IHtmlContent> Search(this IHtmlHelper htmlHelper, string resultsPageUrl, string provider, Nucleus.Modules.Search.ViewModels.Settings.DisplayModes displayMode, string prompt, int maximumSuggestions, Boolean includeFiles, string includeScopes, object htmlAttributes)
		{
			return await SearchRenderer.Build(htmlHelper, resultsPageUrl, provider, displayMode, prompt, maximumSuggestions, includeFiles, includeScopes, htmlAttributes);
		}
	}
}
