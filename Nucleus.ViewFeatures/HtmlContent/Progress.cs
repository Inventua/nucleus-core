using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ViewFeatures.HtmlContent
{
	internal static class Progress
	{

		// <div class="UploadProgress">
		//	<div>
		//		<span class="nucleus-material-icon">&#xe2c6</span><span class="progress-message">Uploading file ...</span>
		//	</div>
		//	<div class="progress">
		//		<div class="progress-bar bg-success" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100"></div>
		//	</div>
		//</div>

		internal static TagBuilder Build(ViewContext context, string caption, string cssClass, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("div");
			outputBuilder.AddCssClass(cssClass ?? "UploadProgress");
			
			TagBuilder labelBuilder = new("label");
			labelBuilder.InnerHtml.Append(caption);
			
			TagBuilder progressBuilder = new("progress");
			progressBuilder.Attributes.Add("value", "0");
			progressBuilder.Attributes.Add("max", "100");
			progressBuilder.InnerHtml.Append("0%");

			outputBuilder.InnerHtml.AppendHtml(labelBuilder);
			outputBuilder.InnerHtml.AppendHtml(progressBuilder);
			
			outputBuilder.MergeAttributes(htmlAttributes);

			return outputBuilder;
		}
	}
}
