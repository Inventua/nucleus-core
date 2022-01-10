using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace Nucleus.ViewFeatures.TagHelpers
{
	/// <summary>
	/// Renders a settings control.
	/// </summary>
	/// <remarks>
	/// A settings control supplements inner content with a <![CDATA[<div>]]> wrapper which has a class of 'settings-control'.  The DIV contains a LABEL 
	/// which contains a SPAN with the specified caption and help text (title attribute).  The SettingsControl is used by the administrative user interface
	/// but can be used by extensions/modules to render input controls with labels and inputs aligned on-screen.
	/// </remarks>
	[HtmlTargetElement("SettingsControl")]
	public class SettingsControlTagHelper : TagHelper
	{
		/// <summary>
		/// Specifies whether to render the label before or after the control content.
		/// </summary>
		public enum RenderModes
		{
			/// <summary>
			/// Render the label first
			/// </summary>
			LabelFirst,
			/// <summary>
			/// Render the label last
			/// </summary>
			LabelLast
		}

		/// <summary>
		/// Specifies whether to render the label before or after the control content.
		/// </summary>
		[HtmlAttributeName("rendermode")]
		public RenderModes RenderMode { get; set; } = RenderModes.LabelFirst;

		/// <summary>
		/// Label text.
		/// </summary>
		[HtmlAttributeName("caption")]
		public string Caption { get; set; }

		/// <summary>
		/// Help text.
		/// </summary>
		[HtmlAttributeName("helptext")]
		public string HelpText { get; set; }

		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = new("div");
			TagBuilder labelBuilder = new("label");
			TagBuilder labelSpanBuilder = new("span");

			output.TagName = "div";

			TagHelperContent content = await output.GetChildContentAsync();
			
			if (output.Attributes.ContainsName("class"))
			{
				output.Attributes.SetAttribute("class", "settings-control " + output.Attributes["class"].Value);
			}
			else
			{
				output.Attributes.Add("class", "settings-control"); 
			}

			if (!String.IsNullOrEmpty(this.HelpText))
			{
				output.Attributes.SetAttribute("title", this.HelpText);
			}

			labelSpanBuilder.InnerHtml.Append(this.Caption);
						
			switch (this.RenderMode)
			{
				case RenderModes.LabelFirst:
					labelBuilder.InnerHtml.AppendHtml(labelSpanBuilder);
					labelBuilder.InnerHtml.AppendHtml(content);
					break;

				case RenderModes.LabelLast:
					labelBuilder.InnerHtml.AppendHtml(content);
					labelBuilder.InnerHtml.AppendHtml(labelSpanBuilder);
					break;
			}

			builder.InnerHtml.AppendHtml(labelBuilder);					
			output.Content.AppendHtml(builder.InnerHtml);
		}


	}
}
