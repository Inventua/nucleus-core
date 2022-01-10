using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Nucleus.ViewFeatures
{
	/// <summary>
	/// Tag Builder extensions used by HtmlHelpers, TagHelpers and internal rendering classes. 
	/// </summary>
	public static class TagBuilderExtensions
	{
		/// <summary>
		/// Convert a string value to a <see cref="T:Microsoft.AspNetCore.Html.HtmlContentBuilder"/> containing the string.
		/// </summary>
		/// <param name="value">String value.</param>
		/// <returns>A HtmlContentBuilder containing the string.</returns>
		public static HtmlContentBuilder ToContent(this string value)
		{
			HtmlContentBuilder content = new HtmlContentBuilder();
			content.Append(value);
			return content;
		}

		/// <summary>
		/// Convert a string value to a <see cref="T:Microsoft.AspNetCore.Html.HtmlContentBuilder"/> containing the string.
		/// </summary>
		/// <param name="value">String value.</param>
		/// <returns>A HtmlContentBuilder containing the string.</returns>
		public static HtmlContentBuilder ToHtmlContent(this string value)
		{
			HtmlContentBuilder content = new HtmlContentBuilder();
			content.AppendHtml(value);
			return content;
		}

		/// <summary>
		/// Merge an anonymous object containing HTML attributes with the attributes of a TagBuilder.  
		/// </summary>
		/// <param name="builder">The TagBuilder to merge the HTML attributes into.</param>
		/// <param name="htmlAttributes">Anonymous object containing HTML attributes.</param>
		public static void MergeAttributes(this TagBuilder builder, object htmlAttributes)
		{
			IDictionary<string, object> attributes = null;
			Boolean isTagHelper = false;

			if (htmlAttributes is IEnumerable<TagHelperAttribute>)
			{
				isTagHelper = true;
				attributes = new Dictionary<string, object>();

				foreach (TagHelperAttribute item in (IEnumerable<TagHelperAttribute>)htmlAttributes)
				{
					attributes.Add(item.Name, item.Value);
				}
			}
			else
			{
				attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			}

			if (attributes != null)
			{
				foreach (KeyValuePair<string, object> prop in attributes)
				{
					if (prop.Value != null)
					{
						// Tag helpers automatically populate the class property, so we only need to do this if called by a Html Helper
						if (prop.Key.ToLower() == "class")
						{
							if (!isTagHelper)
							{
								builder.AddCssClass(prop.Value.ToString());
							}
						}
						else
						{
							builder.Attributes.Add(prop.Key, prop.Value.ToString());
						}
					}
				}
			}
		}
	}
}
