﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;

namespace Nucleus.ViewFeatures
{
  /// <summary>
  /// Extensions used to render links to Nucleus pages, files and actions.
  /// </summary>
  public static class UrlHelperExtensions
	{		
		/// <summary>
		/// Output an relative url, with the leading "~" character for use by UrlHelper.Content for the specified <see cref="Page"/>.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public static string RelativePageLink(Page page)
		{
      ArgumentNullException.ThrowIfNull(page);

      if (page.DefaultPageRoute()?.Path == null || page.Disabled) return "";

      string path;
      switch (page.LinkType)
      {
        case Page.LinkTypes.Url:
          path = page.LinkUrl;
          break;

        default:
          // LinkType:Normal/File/Page.  Link type=File/Page is handled in PageRoutingMiddleware
          if (page.DefaultPageRoute()?.Path == null) return "";
          path = page.DefaultPageRoute().Path;
          // We append a "/" so that if the path contains dots the net core static file provider doesn't interpret the path as a file
          path = $"~" + (path.StartsWith('/') ? "" : "/") + path + (path.EndsWith('/') ? "" : "/");
          break;
      }

      return path;
		}

		/// <summary>
		/// Output an url for the specified <see cref="Page"/>.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="page"></param>
		/// <returns></returns>
		public static string PageLink(this IUrlHelper helper, Page page)
		{
			ArgumentNullException.ThrowIfNull(page);

      return helper.Content(RelativePageLink(page));


   //   if (page.Disabled || page.DefaultPageRoute() == null) return "";
			//string path = page.DefaultPageRoute().Path;

			//// We append a "/" so that if the path contains dots the net core static file provider doesn't interpret the path as a file
			//return helper.Content($"~" + path + (path.EndsWith("/") ? "" : "/"));
		}

		/// <summary>
		/// Output an url for the specified <see cref="Page"/> with <paramref name="relativePath"/> appended."/>.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="page"></param>
		/// <param name="relativePath"></param>
		/// <returns></returns>
		public static string PageLink(this IUrlHelper helper, Page page, string relativePath)
		{
      ArgumentNullException.ThrowIfNull(page);

      if (relativePath.StartsWith('/'))
			{
				relativePath = relativePath[1..];
			}

			return PageLink(helper, page) + relativePath;
		}

		// AG: Removed.  This overload clashes with the .net built-in PageLink extension method.
		///// <summary>
		///// Output an url for the current <see cref="Page"/> with <paramref name="relativePath"/> appended."/>.
		///// </summary>
		///// <param name="helper"></param>
		///// <param name="relativePath"></param>
		///// <returns></returns>
		//public static string PageLink(this IUrlHelper helper, string relativePath)
		//{
		//	Nucleus.Abstractions.Models.Context context = helper.ActionContext.HttpContext.RequestServices.GetService<Nucleus.Abstractions.Models.Context>();

		//	if (relativePath.StartsWith("/"))
		//	{
		//		relativePath = relativePath[1..];
		//	}
		//	return PageLink(helper, context.Page) + relativePath;
		//}

		/// <summary>
		/// Output an url for the specified <see cref="File"/>.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="file"></param>
		/// <returns></returns>
		/// <remarks>
		/// This overload is intended for use as an Url extension method.
		/// </remarks>
		public static string FileLink(this IUrlHelper helper, File file)
		{
			if (file == null) return "";
			return FileLink(helper, file, false, true);
		}

		/// <summary>
		/// Output an url for the specified <see cref="File"/>.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="file"></param>
		/// <param name="inline">
		///	Specifies whether the link should output a content-disposition header which renders the content inline, or as an attachment.  When
		///	a file is output with content-disposition: attachment, browsers typically download the file rather than displaying it.
		/// </param>
		/// <returns></returns>
		/// <remarks>
		/// This overload is intended for use as an Url extension method.
		/// </remarks>
		public static string FileLink(this IUrlHelper helper, File file, Boolean inline)
		{
			if (file == null) return "";
			return FileLink(helper, file, inline, true);
		}


		/// <summary>
		/// Output an url for the specified <see cref="File"/>.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="file"></param>
		/// <param name="inline">
		///	Specifies whether the link should output a content-disposition header which renders the content inline, or as an attachment.  When
		///	a file is output with content-disposition: attachment, browsers typically download the file rather than displaying it.
		/// </param>
		/// <param name="encodePath"></param>
		/// <returns></returns>
		public static string FileLink(this IUrlHelper helper, File file, Boolean inline, Boolean encodePath)
		{
			string inlineParm = "";
			if (inline)
			{
				inlineParm = "?inline=true";
			}

			if (!encodePath)
			{
				return $"files/{file.Provider}/{file.Path}{inlineParm}";
			}
			else
			{
				return $"files/{file.EncodeFileId()}{inlineParm}";
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
		/// Generates a fully qualified URL to an action method by using the specified action name, controller name and area name.
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
			context.RouteName = RoutingConstants.AREA_ROUTE_NAME;

			return helper.RouteUrl(context);
		}

		/// <summary>
		/// Generates a fully qualified URL to an Nucleus extension action method by using the specified action name, controller name and extension name.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="actionName"></param>
		/// <param name="controllerName"></param>
		/// <param name="extensionName"></param>
		/// <returns></returns>
		public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName, string extensionName)
		{
			return NucleusAction(helper, actionName, controllerName, null, extensionName, null);
		}

		/// <summary>
		/// Generates a fully qualified URL to an Nucleus extension action method by using the specified action name, controller name and extension name.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="actionName"></param>
		/// <param name="controllerName"></param>
		/// <param name="extensionName"></param>
		/// <param name="routeValues"></param>
		/// <returns></returns>
		public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName, string extensionName, object routeValues)
		{
			return NucleusAction(helper, actionName, controllerName, null, extensionName, routeValues);
		}

		/// <summary>
		/// Generates a fully qualified URL to an Nucleus extension action method by using the specified action name, controller name and extension name.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="actionName"></param>
		/// <param name="controllerName"></param>
		/// <param name="areaName"></param>
		/// <param name="extensionName"></param>
		/// <returns></returns>
		public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName, string areaName, string extensionName)
		{
			return NucleusAction(helper, actionName, controllerName, areaName, extensionName, null);
		}

		/// <summary>
		/// Generates a fully qualified URL to an Nucleus extension action method by using the specified action name, controller name and extension name.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="actionName"></param>
		/// <param name="controllerName"></param>
		/// <param name="areaName"></param>
		/// <param name="extensionName"></param>
		/// <param name="routeValues"></param>
		/// <returns></returns>
		public static string NucleusAction(this IUrlHelper helper, string actionName, string controllerName, string areaName, string extensionName, object routeValues)
		{
			RouteValueDictionary routeValueDict = new(routeValues);
			UrlRouteContext routeContext = new();
			Nucleus.Abstractions.Models.Context context = helper.ActionContext.HttpContext.RequestServices.GetService<Nucleus.Abstractions.Models.Context>();

			if (context.Module != null)
			{
				routeValueDict.TryAdd("mid", context.Module.Id);
			}
			routeValueDict.TryAdd("extension", extensionName);
			routeValueDict.TryAdd("area", areaName);
			routeValueDict.TryAdd("controller", controllerName);
			routeValueDict.TryAdd("action", actionName);

			routeContext.Values = routeValueDict;

			if (!String.IsNullOrEmpty(areaName))
			{
				routeContext.RouteName = RoutingConstants.AREA_ROUTE_NAME;
			}
			else
			{
				routeContext.RouteName = RoutingConstants.EXTENSIONS_ROUTE_NAME;
			}

			return helper.RouteUrl(routeContext);
		}

		/// <summary>
		/// Generates a fully qualified URL to an Nucleus extension API method by using the specified action name and controller name and extension name.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="actionName"></param>
		/// <param name="controllerName"></param>
		/// <param name="extensionName"></param>
		public static string ApiAction(this IUrlHelper helper, string actionName, string controllerName, string extensionName)
		{
			RouteValueDictionary routeValues = [];
			UrlRouteContext context = new();

			routeValues.Add("extension", extensionName);
			routeValues.Add("controller", controllerName);
			routeValues.Add("action", actionName);

			context.Values = routeValues;
			context.RouteName = RoutingConstants.API_ROUTE_NAME;

			return helper.RouteUrl(context);
		}

		/// <summary>
		/// Returns an absolute Url for the specified local path.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="localPath"></param>
		/// <returns></returns>
		/// <remarks>
		/// If required, callers must call IUriHelper.Content to parse '~' in the path, this function assumes that localPath is an actual
		/// path with no special characters
		/// </remarks>
		public static System.Uri GetAbsoluteUri(this IUrlHelper helper, string localPath)
		{
      string fragment = "";

      if (localPath.Contains('#'))
      {
        fragment = localPath.Substring(localPath.LastIndexOf('#'));
        localPath = localPath.Substring(0, localPath.LastIndexOf('#') - 1);
      }

			if (!localPath.StartsWith('/'))
			{
				localPath = $"/{localPath}";
			}

			if (!localPath.EndsWith('/'))
			{
				localPath = $"{localPath}/";
			}

			UriBuilder builder = new
      (
        helper.ActionContext.HttpContext.Request.Scheme,
        helper.ActionContext.HttpContext.Request.Host.Host, 
        helper.ActionContext.HttpContext.Request.Host.Port ?? -1, 
        helper.ActionContext.HttpContext.Request.PathBase + localPath,
        fragment
      );

			return builder.Uri;
		}
	}
}
