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
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders a tab panel containing content
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	/// <internal />
	/// <hidden />
	internal static class TabPanel
	{
		internal static TagBuilder Build(ViewContext context, string id, Boolean active, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("div");			

			outputBuilder.AddCssClass("tab-pane");

			if (active)
			{
				outputBuilder.AddCssClass("active");
			}

			outputBuilder.Attributes.Add("id", id);

			return outputBuilder;			
		}		
	}
}
