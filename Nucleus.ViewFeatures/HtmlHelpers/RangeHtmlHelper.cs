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
	/// Html helper used to render a Range control.
	/// </summary>
	public static class RangeHtmlHelper
	{
		/// <summary>
		/// Renders a Range control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="expression"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="step"></param>
		/// <param name="htmlAttributes"></param>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="Guid"></typeparam>
		/// <returns></returns>
		public static IHtmlContent RangeFor<TModel, Guid>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, Guid>> expression, double min, double max, double step, object htmlAttributes)
		{
		
			ModelExpressionProvider provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ModelExpressionProvider>();
			ModelExpression modelExpression = provider.CreateModelExpression(htmlHelper.ViewData, expression);
			double value = modelExpression.Model == null ? 0 : (Double)modelExpression.Model;
			
			return Nucleus.ViewFeatures.HtmlContent.Range.Build(htmlHelper.ViewContext, htmlHelper.IdFor(expression), htmlHelper.NameFor(expression), min, max, step, value, htmlAttributes);
		}

		/// <summary>
		/// Renders a Range control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="expression"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="step"></param>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="Guid"></typeparam>
		/// <returns></returns>
		public static IHtmlContent RangeFor<TModel, Guid>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, Guid>> expression, double min, double max, double step)
		{

			ModelExpressionProvider provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ModelExpressionProvider>();
			ModelExpression modelExpression = provider.CreateModelExpression(htmlHelper.ViewData, expression);
			double value = modelExpression.Model == null ? 0 : Convert.ToDouble(modelExpression.Model);

			return Nucleus.ViewFeatures.HtmlContent.Range.Build(htmlHelper.ViewContext, htmlHelper.IdFor(expression), htmlHelper.NameFor(expression), min, max, step, value, null);
		}

	}
}
