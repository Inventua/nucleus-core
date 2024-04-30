using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace Nucleus.ViewFeatures.TagHelpers
{
	/// <summary>
	/// Returns a modal dialog.
	/// </summary>	
	[HtmlTargetElement("Modal")]
	public class ModalTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// The modal dialog title.
		/// </summary>
		public string Title { get; set; }

    /// <summary>
    /// Css class
    /// </summary>
    /// <example>
    /// <![CDATA[<modal id="my-dialog" modal-class="modal-full-size"></modal>]]>
    /// </example>
    [HtmlAttributeName("modal-class")]
    public string ModalClass { get; set; }

    /// <summary>
    /// Specifies whether to add a .nucleus-admin-content class to the modal content div.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    /// <example>
    /// <![CDATA[<modal id="my-dialog" use-admin-styles="false"></modal>]]>
    /// </example>
    [HtmlAttributeName("use-admin-styles")]
    public Boolean UseAdminStyles { get; set; } = true;

    /// <summary>
    /// Specifies whether to include a dialog footer section.
    /// </summary>
    /// <remarks>
    /// Defaults to false.
    /// </remarks>
    /// <example>
    /// <![CDATA[<modal id="my-dialog" render-footer="true"></modal>]]>
    /// </example>
    [HtmlAttributeName("render-footer")]
    public Boolean RenderFooter { get; set; }

    /// <summary>
    /// Specifies whether the modal dialog has a close button.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    /// <example>
    /// <![CDATA[<modal id="my-dialog" can-close="true"></modal>]]>
    /// </example>
    [HtmlAttributeName("can-close")]
    public Boolean CanClose { get; set; } = true;

		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagHelperContent content = await output.GetChildContentAsync();

			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.Modal.Build(this.ViewContext, this.Title, this.CanClose, content, this.ModalClass, this.RenderFooter, this.UseAdminStyles, null);

			if (builder == null)
			{
				output.SuppressOutput();
			}
			else
			{
				output.TagMode = TagMode.StartTagAndEndTag;
				output.TagName = builder.TagName;


				output.MergeAttributes(builder);
				output.Content.AppendHtml(builder.InnerHtml);
			}

			await Task.CompletedTask;
		}
	}
}