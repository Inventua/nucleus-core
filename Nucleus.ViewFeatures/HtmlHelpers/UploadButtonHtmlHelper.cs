using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Models;
using System.Linq.Expressions;


namespace Nucleus.ViewFeatures.HtmlHelpers
{
	/// <summary>
	/// Html helper used to render an upload button.
	/// </summary>
	public static class UploadButtonHtmlHelper
	{
    /// <summary>
		/// Returns an upload button with a label and default glyph.
		/// </summary>
		/// <param name="htmlHelper"></param>
    /// <param name="name"></param>
		/// <param name="caption"></param>
    /// <param name="accept"></param>
    /// <param name="allowMultiple"></param>
		/// <param name="formaction"></param>
    /// <param name="dataTarget"></param>
    /// <param name="cssClass"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent UploadButton(this IHtmlHelper htmlHelper, string name, string caption, string accept, Boolean allowMultiple, string formaction, string dataTarget, string cssClass, object htmlAttributes = null)
    {
      return Nucleus.ViewFeatures.HtmlContent.UploadButton.Build(htmlHelper.ViewContext, "&#xe2c6", name, caption, accept, allowMultiple, formaction, dataTarget, cssClass, htmlAttributes);
    }

    /// <summary>
		/// Returns an upload button with a label and default glyph.
		/// </summary>
		/// <param name="htmlHelper"></param>
    /// <param name="name"></param>
		/// <param name="caption"></param>
    /// <param name="accept"></param>
    /// <param name="allowMultiple"></param>
		/// <param name="formaction"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent UploadButton(this IHtmlHelper htmlHelper, string name, string caption, string accept, Boolean allowMultiple, string formaction, object htmlAttributes = null)
    {
      return Nucleus.ViewFeatures.HtmlContent.UploadButton.Build(htmlHelper.ViewContext, "&#xe2c6", name, caption, accept, allowMultiple, formaction, "", "", htmlAttributes);
    }

    /// <summary>
    /// Html helper used to render an upload button.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="glyph"></param>
    /// <param name="name"></param>
		/// <param name="caption"></param>
    /// <param name="accept"></param>
    /// <param name="allowMultiple"></param>
		/// <param name="formaction"></param>
    /// <param name="dataTarget"></param>
    /// <param name="cssClass"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static IHtmlContent UploadButton(this IHtmlHelper htmlHelper, string glyph, string name, string caption, string accept, Boolean allowMultiple, string formaction, string dataTarget, string cssClass, object htmlAttributes = null)
		{
			return Nucleus.ViewFeatures.HtmlContent.UploadButton.Build(htmlHelper.ViewContext, glyph, name, caption, accept, allowMultiple, formaction, dataTarget, cssClass, htmlAttributes);
		}

    /// <summary>
		/// Returns a upload button with a label and glyph.
		/// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="expression"></param>
		/// <param name="glyph"></param>
    /// <param name="caption"></param>
    /// <param name="accept"></param>
    /// <param name="allowMultiple"></param>
    /// <param name="formaction"></param>
    /// <param name="dataTarget"></param>
    /// <param name="cssClass"></param>
    /// <param name="htmlAttributes"></param>
		/// <returns></returns>
		/// <remarks>
		/// This helper is used by the 'Pages' index to expand child page selections. 
		/// </remarks>
		public static IHtmlContent UploadButtonFor<TModel, IFormFile>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, IFormFile>> expression, string glyph, string caption, string accept, Boolean allowMultiple, string formaction, string dataTarget, string cssClass, object htmlAttributes = null)
    {
      ModelExpressionProvider provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ModelExpressionProvider>();
      ModelExpression modelExpression = provider.CreateModelExpression(htmlHelper.ViewData, expression);
      return Nucleus.ViewFeatures.HtmlContent.UploadButton.Build(htmlHelper.ViewContext, glyph, htmlHelper.NameFor(expression), caption, accept, allowMultiple, formaction, dataTarget, cssClass, htmlAttributes);
    }
  }
}
