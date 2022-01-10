using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Linq;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders a PageMenu control.
	/// </summary>
	internal static class Range
	{
		internal static TagBuilder Build(ViewContext context, string propertyId, string propertyName, double min, double max, double step, double value, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("input");

			outputBuilder.Attributes.Add("id", propertyId);
			outputBuilder.Attributes.Add("name", propertyName);

			outputBuilder.Attributes.Add("type", "range");
			outputBuilder.Attributes.Add("min", min.ToString());
			outputBuilder.Attributes.Add("max", max.ToString());
			outputBuilder.Attributes.Add("step", step.ToString());
			
			outputBuilder.Attributes.Add("value", value.ToString());
			outputBuilder.Attributes.Add("title", value.ToString());

			outputBuilder.Attributes.Add("onchange", "jQuery(this).attr('title', jQuery(this).val());");

			return outputBuilder;
		}
	}
}


