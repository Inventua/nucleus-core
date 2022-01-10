using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.ViewFeatures
{	
	public static class UrlHelperExtensions
	{
		public static string PageLink(this IUrlHelper helper, Page page)
		{
			if (page == null || page.DefaultPageRoute() == null) return "";
			string path = page.DefaultPageRoute().Path;

			// We append a "/" so that if the path contains dots the net core static file provider doesn't interpret the path as a file
			return path + (path.EndsWith("/") ? "" : "/");
		}

		public static string DownloadLink(this IUrlHelper helper, File file)
		{
			if (file == null || String.IsNullOrEmpty(file.Provider)) return "";
			return DownloadLink(helper, file.Provider, file.Path, false, true);
		}

		public static string DownloadLink(this IUrlHelper helper, File file, Boolean inline)
		{
			if (file == null || String.IsNullOrEmpty(file.Provider)) return "";
			return DownloadLink(helper, file.Provider, file.Path, inline, true);
		}

		//public static string DownloadLink(this IUrlHelper helper, string providerKey, string path)
		//{
		//	return DownloadLink(helper, providerKey, path, false, true);
		//}

		//public static string DownloadLink(this IUrlHelper helper, string providerKey, string path, Boolean inline)
		//{
		//	return DownloadLink(helper, providerKey, path, inline, true);
		//}

		private static string DownloadLink(this IUrlHelper helper, string providerKey, string path, Boolean inline, Boolean encodePath)
		{
			string inlineParm = ""; 
			if (inline)
			{
				inlineParm = "?inline=true";
			}

			if (!encodePath)
			{
				return $"/files/{providerKey}/{path}{inlineParm}";
			}
			else
			{
				string encodedPath = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{providerKey}/{path}"));
				return $"/files/{encodedPath}{inlineParm}";
			}
		}


		/// <summary>
		/// Generates a fully qualified URL to an action method by using the specified action name, controller name and area name.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="actionName">The action name.</param>
		/// <param name="controllerName">The controller name.</param>
		/// <param name="areaName">The area name.</param>
		/// <returns></returns>
		public static string AreaAction(this IUrlHelper helper, string actionName, string controllerName, string areaName)
		{
			return AreaAction(helper, actionName, controllerName, areaName, null);
		}

		/// <summary>
		/// /// Generates a fully qualified URL to an action method by using the specified action name, controller name and area name.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="actionName">The action name.</param>
		/// <param name="controllerName">The controller name.</param>
		/// <param name="areaName">The area name.</param>
		/// <param name="routeValues">An object that contains the parameters for a route.</param>
		/// <returns></returns>
		public static string AreaAction(this IUrlHelper helper, string actionName, string controllerName, string areaName, object routeValues)
		{
			RouteValueDictionary routeDictionary = new(routeValues);
			UrlRouteContext context = new();

			routeDictionary.Add("area", areaName);
			routeDictionary.Add("controller", controllerName);
			routeDictionary.Add("action", actionName);

			context.Values = routeDictionary;
			context.RouteName = Constants.AREA_ROUTE_NAME;

			return helper.RouteUrl(context);
		}

		//public static string NucleusAction(this IUrlHelper helper, string actionName)
		//{
		//	// This doesn't work properly in a module view/edit view.  IUrlHelper is always initialized with the current httpRequest, so
		//	// ActionContext always returns the original request route values rather than the ones which are set in ModuleContentRenderer.
		//	string controllerName = (string)helper.ActionContext.HttpContext.GetRouteValue("controller");
		//	return NucleusAction(helper, actionName, controllerName, null);
		//}
		//public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName)
		//{
		//	return NucleusAction(helper, actionName, controllerName, null, (string)helper.ActionContext.RouteData.Values["extension"]);
		//}

		public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName, string extensionName)
		{
			return NucleusAction(helper, actionName, controllerName, null, extensionName, null);
		}

		public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName, string extensionName, object routeValues)
		{
			return NucleusAction(helper, actionName, controllerName, null, extensionName, routeValues);
		}

		public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName, string areaName, string extensionName)
		{
			return NucleusAction(helper, actionName, controllerName, areaName, extensionName, null);
		}

		public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName, string areaName, string extensionName, object routeValues)
		{
			RouteValueDictionary routeValueDict = new(routeValues);
			UrlRouteContext routeContext = new();
			Nucleus.Abstractions.Models.Context context = helper.ActionContext.HttpContext.RequestServices.GetService<Nucleus.Abstractions.Models.Context>();

			if (context.Module != null)
			{
				routeValueDict.Add("mid", context.Module.Id);
			}
			routeValueDict.Add("extension", extensionName);
			routeValueDict.Add("area", areaName);
			routeValueDict.Add("controller", controllerName);
			routeValueDict.Add("action", actionName);

			routeContext.Values = routeValueDict;

			if (!String.IsNullOrEmpty(areaName))
			{
				routeContext.RouteName = Constants.AREA_ROUTE_NAME;
			}
			else
			{
				routeContext.RouteName = Constants.EXTENSIONS_ROUTE_NAME;
			}

			return helper.RouteUrl(routeContext);
		}

		public static string ApiAction(this IUrlHelper helper, string actionName, string controllerName)
		{
			return ApiAction(helper, actionName, controllerName, null);
		}

		public static string ApiAction(this IUrlHelper helper, string actionName, string controllerName, string areaName)
		{
			RouteValueDictionary routeValues = new(); ;
			UrlRouteContext context = new();

			routeValues.Add("area", areaName);
			routeValues.Add("controller", controllerName);
			routeValues.Add("action", actionName);

			context.Values = routeValues;
			context.RouteName = Constants.API_ROUTE_NAME;

			return helper.RouteUrl(context);
		}

		public static string ResolveExtensionUrl(this IUrlHelper helper, string url)
		{
			if (url.StartsWith(Constants.VIEWPATH_TOKEN))
			{
				string executingViewPath = ((Microsoft.AspNetCore.Mvc.Rendering.ViewContext)helper.ActionContext).ExecutingFilePath;
				System.Uri viewPath = GetAbsoluteUri(helper, System.IO.Path.GetDirectoryName(executingViewPath).Replace("\\", "/"));
				return new System.Uri(viewPath, ParseScriptPath(url, Constants.VIEWPATH_TOKEN)).AbsolutePath;
			}
			else if (url.StartsWith(Constants.EXTENSIONPATH_TOKEN))
			{
				string viewPath = System.IO.Path.GetDirectoryName(((Microsoft.AspNetCore.Mvc.Rendering.ViewContext)helper.ActionContext).ExecutingFilePath).Replace("\\", "/");
				string[] viewPathParts = viewPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

				// viewPathParts[0] will be "extensions", and viewPathParts[1] will be the extension folder name
				if (viewPathParts.Length > 2)
				{
					System.Uri extensionPath = GetAbsoluteUri(helper, $"{viewPathParts[0]}/{viewPathParts[1]}");
					return new System.Uri(extensionPath, ParseScriptPath(url, Constants.EXTENSIONPATH_TOKEN)).AbsolutePath;
				}
			}			

			return url;			
		}

		private static string ParseScriptPath(string url, string token)
		{
			string scriptPath = url[token.Length..];
			if (scriptPath.StartsWith('/'))
			{
				scriptPath = scriptPath[1..];
			}
			return scriptPath;
		}

		public static System.Uri GetAbsoluteUri(this IUrlHelper helper, string localPath)
		{
			if (!localPath.StartsWith('/'))
			{
				localPath = $"/{localPath}";
			}

			if (!localPath.EndsWith('/'))
			{
				localPath = $"{localPath}/";
			}

			UriBuilder builder = new(helper.ActionContext.HttpContext.Request.Scheme, helper.ActionContext.HttpContext.Request.Host.Host, helper.ActionContext.HttpContext.Request.Host.Port.Value, helper.ActionContext.HttpContext.Request.PathBase + localPath);

			return builder.Uri;
		}
	}
}
