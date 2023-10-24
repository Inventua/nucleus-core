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
	/// <internal />
	/// <hidden />
	internal static class Range
	{
		internal static TagBuilder Build(ViewContext context, string propertyId, string propertyName, double min, double max, double step, double value, object htmlAttributes)
		{
      TagBuilder outputBuilder = new("div");
      outputBuilder.AddCssClass("d-flex gap-1 nucleus-range");

      TagBuilder rangeBuilder = new("input");
			rangeBuilder.Attributes.Add("id", $"{propertyId}_range");
			//rangeBuilder.Attributes.Add("name", propertyName);

			rangeBuilder.Attributes.Add("type", "range");
			rangeBuilder.Attributes.Add("min", min.ToString());
			rangeBuilder.Attributes.Add("max", max.ToString());
			rangeBuilder.Attributes.Add("step", step.ToString());
			
			rangeBuilder.Attributes.Add("value", value.ToString());
			rangeBuilder.Attributes.Add("title", value.ToString());

			rangeBuilder.Attributes.Add("onchange", $"jQuery(this).attr('title', jQuery(this).val()); jQuery('#{propertyId}').val(jQuery(this).val());");
      rangeBuilder.AddCssClass("flex-1");

      TagBuilder textboxBuilder = new("input");
      textboxBuilder.Attributes.Add("id", propertyId);
      textboxBuilder.Attributes.Add("name", propertyName);

      textboxBuilder.Attributes.Add("type", "number");
      textboxBuilder.Attributes.Add("min", min.ToString());
      textboxBuilder.Attributes.Add("max", max.ToString());
      textboxBuilder.Attributes.Add("step", step.ToString());
			
      textboxBuilder.Attributes.Add("size", max.ToString("0").Length.ToString());
      textboxBuilder.Attributes.Add("value", value.ToString());

      // setting the text box value to the range control value after setting the range control serves to "round" the value to 
      // the "step" value of the range, and also enforces the max/min.
      textboxBuilder.Attributes.Add("onchange", $"jQuery('#{propertyId}_range').val(jQuery(this).val()); jQuery(this).val(jQuery('#{propertyId}_range').val());");
      
      outputBuilder.InnerHtml.AppendHtml(rangeBuilder);
      outputBuilder.InnerHtml.AppendHtml(textboxBuilder);

      return outputBuilder;

    }
	}
}


