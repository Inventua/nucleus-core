using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.ViewFeatures;
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	public static class BeginNucleusFormHtmlHelper
	{
		/// <summary>
		/// Renders a <![CDATA[<form>]]> start tag to the response. When the user submits the form, the action with name actionName, controllerName
		/// and areaName will process the request.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="actionName">The action name.</param>
		/// <param name="controllerName">The controller name.</param>
		/// <param name="areaName">The area name.</param>
		/// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
		/// <returns></returns>
		//public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, string areaName, FormMethod method)
		//{
		//	return BeginNucleusForm(html, actionName, controllerName, areaName, method, null);
		//}

		public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, FormMethod method)
		{
			return BeginNucleusForm(html, actionName, controllerName, (string)html.ViewContext.RouteData.Values["extension"], method, null);
		}

		public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, FormMethod method, object htmlAttributes)
		{
			return BeginNucleusForm(html, actionName, controllerName, (string)html.ViewContext.RouteData.Values["extension"], method, htmlAttributes);
		}

		/// <summary>
		/// Renders a <![CDATA[<form>]]> start tag to the response. When the user submits the form, the action with name actionName, controllerName
		/// and areaName will process the request.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="actionName">The action name.</param>
		/// <param name="controllerName">The controller name.</param>
		/// <param name="extensionName">The extension name.</param>
		/// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
		/// <param name="htmlAttributes">
		/// An <see cref="Object"/> that contains the HTML attributes for the element. Alternatively, an <see cref="IDictionary<TKey,TValue>"/> instance containing the HTML attributes.
		/// </param>
		/// <returns></returns>
		public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, string extensionName, FormMethod method, object htmlAttributes)
		{
			return BeginNucleusForm(html, actionName, controllerName, null, extensionName, null, method, true, htmlAttributes);
		}

		//public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, FormMethod method, object htmlAttributes)
		//{
		//	return BeginNucleusForm(html, actionName, controllerName, null, null, null, method, true, htmlAttributes);
		//}

		/// <summary>
		/// Renders a <![CDATA[<form>]]> start tag to the response. When the user submits the form, the action with name actionName, controllerName
		/// and areaName will process the request.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="actionName">The action name.</param>
		/// <param name="controllerName">The controller name.</param>
		/// <param name="areaName">The area name.</param>
		/// <param name="routeValues">
		/// An <see cref="Object"/> that contains the parameters for a route. The parameters are retrieved through reflection by examining the 
		/// properties of the Object. This Object is typically created using Object initializer syntax. Alternatively, an 
		/// <see cref="IDictionary<TKey,TValue>"/> instance containing the route parameters.
		/// </param>
		/// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
		/// <param name="htmlAttributes">
		/// An <see cref="Object"/> that contains the HTML attributes for the element. Alternatively, an <see cref="IDictionary<TKey,TValue>"/> instance containing the HTML attributes.
		/// </param>
		/// <returns></returns>
		//public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, string areaName, string extensionName, object routeValues, FormMethod method, object htmlAttributes)
		//{
		//	return BeginNucleusForm(html, actionName, controllerName, areaName, extensionName, routeValues, method, true, htmlAttributes);
		//}

		public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, string extensionName, object routeValues, FormMethod method, object htmlAttributes)
		{
			return BeginNucleusForm(html, actionName, controllerName, null, extensionName, routeValues, method, true, htmlAttributes);
		}

		//public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, string extensionName, object routeValues, FormMethod method, Boolean antiforgery, object htmlAttributes)
		//{
		//return BeginNucleusForm(html, actionName, controllerName, null, extensionName, routeValues, method, antiforgery, htmlAttributes);
		//}

		public static MvcForm BeginNucleusForm(this IHtmlHelper html, string actionName, string controllerName, string areaName, string extensionName, object routeValues, FormMethod method, Boolean antiforgery, object htmlAttributes)
		{
			RouteValueDictionary routeValueDict = new RouteValueDictionary(routeValues);
			Nucleus.Abstractions.Models.Context context = html.ViewContext.HttpContext.RequestServices.GetService<Nucleus.Abstractions.Models.Context>();

			routeValueDict.Add("mid", context.Module.Id);
			routeValueDict.Add("extension", extensionName);
			routeValueDict.Add("area", areaName);
			routeValueDict.Add("controller", controllerName);
			routeValueDict.Add("action", actionName);

			return html.BeginRouteForm(Constants.EXTENSIONS_ROUTE_NAME, routeValueDict, method, antiforgery, htmlAttributes);
		}
	}
}
