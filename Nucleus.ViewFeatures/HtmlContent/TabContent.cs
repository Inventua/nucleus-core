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
	/// Renders tab content wrapper.
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	internal static class TabContent
	{
		internal static TagBuilder Build(ViewContext context, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("div");
			
			outputBuilder.AddCssClass("tab-content");
			
			return outputBuilder;			
		}		
	}
}
