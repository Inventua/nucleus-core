using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;


//<div>
//  <label class="btn btn-secondary">
//		<span class="nucleus-material-icon">&#xe147</span> Add Document
//		<input type="submit" class="collapse" formaction="@Url.NucleusAction("Create", "Documents", "Documents")" data-target="#DocumentEditor">
//	</label>
//</div>

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders an upload control.
	/// </summary>
	/// <internal />
	/// <hidden />
	internal static class UploadButton
	{
		internal static TagBuilder Build(ViewContext context, string glyph, string name, string caption, string accept, Boolean allowMultiple, string formaction, string dataTarget, string cssClass, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
						
			TagBuilder outputBuilder = new("label");
			TagBuilder spanBuilder = new("span");
      TagBuilder inputBuilder = new("input");

      Boolean blnSuppressAutoClass = false;

			object className = TagBuilderExtensions.GetAttribute(htmlAttributes, "class");
			if (className != null && className is string)
			{
				if ((className as string).Contains("btn"))
				{
					blnSuppressAutoClass = true;
				}
			}

      if (!String.IsNullOrEmpty(cssClass))
      {
        outputBuilder.AddCssClass(cssClass);
        blnSuppressAutoClass = true;
      }

			if (!blnSuppressAutoClass)
			{
        outputBuilder.AddCssClass("btn btn-secondary");
			}

			outputBuilder.MergeAttributes(htmlAttributes);

			if (!String.IsNullOrEmpty(glyph))
			{
				spanBuilder.InnerHtml.SetHtmlContent(glyph);
				spanBuilder.AddCssClass("nucleus-material-icon me-1");
        outputBuilder.InnerHtml.AppendHtml(spanBuilder); 	
			}

      inputBuilder.Attributes.Add("name", name);
      if (!String.IsNullOrEmpty(accept))
      {
        inputBuilder.Attributes.Add("accept", accept);
      }
      inputBuilder.Attributes.Add("formaction", urlHelper.Content(formaction));
      inputBuilder.Attributes.Add("class", "collapse");
      inputBuilder.Attributes.Add("type", "file");
      if (allowMultiple)
      {
        inputBuilder.Attributes.Add("multiple", "multiple");
      }

      if (!String.IsNullOrEmpty(caption))
			{
        outputBuilder.InnerHtml.Append(caption);
			}

      if (!String.IsNullOrEmpty(dataTarget))
      {
        inputBuilder.Attributes.Add("data-target", dataTarget);
      }

      outputBuilder.InnerHtml.AppendHtml(inputBuilder); 

			return outputBuilder;			
		}		
	}
}
