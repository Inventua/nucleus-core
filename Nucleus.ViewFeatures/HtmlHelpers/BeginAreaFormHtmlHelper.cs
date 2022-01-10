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

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	/// <summary>
	/// Html helper used to render a form with an area action.
	/// </summary>
	public static class BeginAreaFormHtmlHelper
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
		public static MvcForm BeginAreaForm(this IHtmlHelper html, string actionName, string controllerName, string areaName, FormMethod method)
		{
			return BeginAreaForm(html, actionName, controllerName, areaName, method, null);
		}

		/// <summary>
		/// Renders a <![CDATA[<form>]]> start tag to the response. When the user submits the form, the action with name actionName, controllerName
		/// and areaName will process the request.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="actionName">The action name.</param>
		/// <param name="controllerName">The controller name.</param>
		/// <param name="areaName">The area name.</param>
		/// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
		/// <param name="htmlAttributes">
		/// An <see cref="Object"/> that contains the HTML attributes for the element. Alternatively, an <see cref="IDictionary&lt;TKey,TValue&gt;"/> instance containing the HTML attributes.
		/// </param>
		/// <returns></returns>
		public static MvcForm BeginAreaForm(this IHtmlHelper html, string actionName, string controllerName, string areaName, FormMethod method, object htmlAttributes)
		{
			return BeginAreaForm(html, actionName, controllerName, areaName, null, method, htmlAttributes);
		}

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
		/// <see cref="IDictionary&lt;TKey,TValue&gt;"/> instance containing the route parameters.
		/// </param>
		/// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
		/// <param name="htmlAttributes">
		/// An <see cref="Object"/> that contains the HTML attributes for the element. Alternatively, an <see cref="IDictionary&lt;TKey,TValue&gt;"/> instance containing the HTML attributes.
		/// </param>
		/// <returns></returns>
		public static MvcForm BeginAreaForm(this IHtmlHelper html, string actionName, string controllerName, string areaName, object routeValues, FormMethod method, object htmlAttributes)
		{
			RouteValueDictionary routeValueDict = new RouteValueDictionary(routeValues);

			routeValueDict.Add("area", areaName);
			routeValueDict.Add("controller", controllerName);
			routeValueDict.Add("action", actionName);

			return html.BeginRouteForm(RoutingConstants.AREA_ROUTE_NAME, routeValueDict, method, true, htmlAttributes);
		}
	}
}
