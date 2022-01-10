using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Models;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	/// <summary>
	/// Html helper used to render a PageList control.
	/// </summary>
	public static class PageListHtmlHelper
	{
		/// <summary>
		/// Renders a PageList control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="expression"></param>
		/// <param name="pageMenu"></param>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="Guid"></typeparam>
		/// <returns></returns>
		/// <remarks>
		/// The PageList control is used by the admin control panel (page editor and site editor) and requires supporting code in the hosting 
		/// page and controller.  It will not work standalone.
		/// </remarks>
		public static IHtmlContent PageListFor<TModel, Guid>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, Guid>> expression, Nucleus.Abstractions.Models.PageMenu pageMenu)
		{

			ModelExpressionProvider provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ModelExpressionProvider>();
			ModelExpression modelExpression = provider.CreateModelExpression(htmlHelper.ViewData, expression);
			System.Guid selectedPage = modelExpression.Model == null ? System.Guid.Empty : (System.Guid)modelExpression.Model;

			return Nucleus.ViewFeatures.HtmlContent.PageList.Build(htmlHelper.ViewContext, htmlHelper.IdFor(expression), htmlHelper.NameFor(expression), selectedPage, null, pageMenu, null).Result;
		}

		/// <summary>
		/// Renders a PageList control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="expression"></param>
		/// <param name="pageMenu"></param>
		/// <param name="disabledPageId"></param>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="Guid"></typeparam>
		/// <returns></returns>
		/// <remarks>
		/// The PageList control is used by the admin control panel (page editor and site editor) and requires supporting code in the hosting 
		/// page and controller.  It will not work standalone.
		/// </remarks>
		public static IHtmlContent PageListFor<TModel, Guid>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, Guid>> expression, Nucleus.Abstractions.Models.PageMenu pageMenu, System.Guid disabledPageId)
		{		
			ModelExpressionProvider provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ModelExpressionProvider>();
			ModelExpression modelExpression = provider.CreateModelExpression(htmlHelper.ViewData, expression);
			System.Guid selectedPage = modelExpression.Model == null ? System.Guid.Empty : (System.Guid)modelExpression.Model;
			
			return Nucleus.ViewFeatures.HtmlContent.PageList.Build(htmlHelper.ViewContext, htmlHelper.IdFor(expression), htmlHelper.NameFor(expression), selectedPage, disabledPageId, pageMenu, null).Result;
		}

		/// <summary>
		/// Renders a PageList control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="expression"></param>
		/// <param name="pageMenu"></param>
		/// <param name="disabledPageId"></param>
		/// <param name="htmlAttributes"></param>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="Guid"></typeparam>
		/// <returns></returns>
		/// <remarks>
		/// The PageList control is used by the admin control panel (page editor and site editor) and requires supporting code in the hosting 
		/// page and controller.  It will not work standalone.
		/// </remarks>
		public static IHtmlContent PageListFor<TModel, Guid>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, Guid>> expression, Nucleus.Abstractions.Models.PageMenu pageMenu, System.Guid disabledPageId, object htmlAttributes)
		{
			ModelExpressionProvider provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ModelExpressionProvider>();
			ModelExpression modelExpression = provider.CreateModelExpression(htmlHelper.ViewData, expression);
			System.Guid selectedPage = modelExpression.Model == null ? System.Guid.Empty : (System.Guid)modelExpression.Model;

			return Nucleus.ViewFeatures.HtmlContent.PageList.Build(htmlHelper.ViewContext, htmlHelper.IdFor(expression), htmlHelper.NameFor(expression), selectedPage, disabledPageId, pageMenu, htmlAttributes).Result;
		}
	}
}
