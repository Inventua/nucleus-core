using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	public static class AreaActionLinkHtmlHelper
	{
		/// <summary>
		/// Returns an anchor (<![CDATA[a]]>) element that contains a URL path to the specified action, controller and area.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="text">The inner text of the anchor element.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="areaName">The name of the area.</param>
		/// <param name="routeValues">
		/// An <see cref="Object"/> that contains the parameters for a route. The parameters are retrieved through reflection by examining the 
		/// properties of the Object. This Object is typically created using Object initializer syntax. Alternatively, an 
		/// <see cref="IDictionary<TKey,TValue>"/> instance containing the route parameters.
		/// </param>
		/// <param name="htmlAttributes">
		/// An <see cref="Object"/> that contains the HTML attributes for the element. Alternatively, an <see cref="IDictionary<TKey,TValue>"/> instance containing the HTML attributes.
		/// </param>
		/// <returns></returns>
		public static IHtmlContent AreaActionLink(this IHtmlHelper html, string text, string actionName, string controllerName, string areaName, object routeValues, object htmlAttributes)
		{
			RouteValueDictionary routeValueDict = new RouteValueDictionary(routeValues);

			routeValueDict.Add("area", areaName);
			routeValueDict.Add("controller", controllerName);
			routeValueDict.Add("action", actionName);

			return html.RouteLink(text, Constants.AREA_ROUTE_NAME, routeValueDict, htmlAttributes);
		}
	}
}
