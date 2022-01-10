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
	public static class HtmlEditorHtmlHelper
	{
		public static IHtmlContent AddHtmlEditor(this IHtmlHelper htmlHelper)
		{
			HtmlEditorOptions options = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IOptions<HtmlEditorOptions>>().Value;

			foreach (HtmlEditorScript option in options.Scripts)
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

			return new HtmlContentBuilder();
		}
	}
}
