using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Nucleus.ViewFeatures.TagHelpers
{
  /// <summary>
  /// Controls rendering of a data-inline-edit-route attribute on an element, depending on whether the user is in 
  /// content edit mode.  Client-side components use the data-inline-edit-route attribute to handle inline editing,
  /// </summary>
  [HtmlTargetElement(HtmlTargetElementAttribute.ElementCatchAllTarget, Attributes = "[inline-edit-route]")]
	public class InlineEditorTagHelper : TagHelper
	{
    private static string[] HTML_ELEMENTS = { "div", "p", "section" };

    /// <summary>
    /// Enum used to specify options for the inline editor in the <see cref="InlineEditMode"/> property.
    /// </summary>
    public enum InlineEditModes
    {
      /// <summary>
      /// Automatically detect which mode to use based on the element type.  
      /// </summary>
      Auto,
      /// <summary>
      /// The edited element can not contain more than one line and cannot contain any html markup.
      /// </summary>
      SingleLineText,
      /// <summary>
      /// The edited element can contain more than one line but cannot contain any html markup.
      /// </summary>
      MultiLineText,
      /// <summary>
      /// The edited element can contain more than one line and contain HTML markup.
      /// </summary>
      Html
    }

    /// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Specifies whether to suppress an empty element.
    /// </summary>
    [HtmlAttributeName("inline-edit-route")]
		public string InlineEditRoute { get; set; }

    /// <summary>
    /// Specifies options for the inline editor.
    /// </summary>
    [HtmlAttributeName("inline-edit-mode")]
    public InlineEditModes InlineEditMode { get; set; } = InlineEditModes.Auto;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
      Context nucleusContext = this.ViewContext.HttpContext.RequestServices.GetService<Context>();
      
      if (!String.IsNullOrEmpty(this.InlineEditRoute) && this.ViewContext.HttpContext.User.IsEditing(this.ViewContext.HttpContext, nucleusContext.Site, nucleusContext.Page, nucleusContext.Module))
			{
        output.Attributes.Add("data-inline-edit-route", this.InlineEditRoute);

        if (this.InlineEditMode == InlineEditModes.Auto)
        {
          if (HTML_ELEMENTS.Contains(context.TagName, StringComparer.OrdinalIgnoreCase))
          {
            this.InlineEditMode = InlineEditModes.MultiLineText;
          }
          else
          {
            this.InlineEditMode = InlineEditModes.SingleLineText;
          }
        }

        output.Attributes.Add("data-inline-edit-mode", this.InlineEditMode.ToString());

        // if the mode is single line, ensure that the inner content does not have leading or trailing line feeds, tabs or spaces, which can
        // be the case when editable content has newlines and is formatted in views.  We need to do this to make sure that the white space 
        // is not added to the content and saved when the inline editing functionality is used.
        if (this.InlineEditMode == InlineEditModes.SingleLineText)
        {
          TagHelperContent content = await output.GetChildContentAsync();
          string originalValue = content.GetContent();
          string trimmedValue = originalValue.Trim();
          if (originalValue != trimmedValue)
          {
            output.Content.SetContent(content.GetContent().Trim());
          }
        }
      }     

      await Task.CompletedTask;
		}
	}
}
