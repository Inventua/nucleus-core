using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;

namespace Nucleus.ViewFeatures
{
	/// <summary>
	/// Extensions which use the HtmlHelper class.
	/// </summary>
	public static class HtmlHelperExtensions
	{
		/// <summary>
		/// Special token used by the AddScript and AddStyle html helpers to represent the path of the currently executing view.
		/// </summary>
		public static string VIEWPATH_TOKEN = "~!";

		/// <summary>
		/// Special token used by the AddScript and AddStyle html helpers to represent the extension path of the currently executing view.
		/// </summary>
		public static string EXTENSIONPATH_TOKEN = "~#";

		/// <summary>
		/// Generates an absolute url from the passed-in relative url after substituting ~! for the currently executing view path, or ~#
		/// for the currently executing extension.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="url"></param>
		/// <returns></returns>
		public static string ResolveExtensionUrl(this IHtmlHelper helper, string url)
		{
			if (url.StartsWith(VIEWPATH_TOKEN))
			{
				IUrlHelper urlHelper = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(helper.ViewContext);

				string executingViewPath = (helper.ViewContext.View).Path;
				System.Uri viewPath = urlHelper.GetAbsoluteUri(System.IO.Path.GetDirectoryName(executingViewPath).Replace("\\", "/"));
				return new System.Uri(viewPath, ParseScriptPath(url, VIEWPATH_TOKEN)).AbsolutePath;
			}
			else if (url.StartsWith(EXTENSIONPATH_TOKEN))
			{
				IUrlHelper urlHelper = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(helper.ViewContext);

				string viewPath = System.IO.Path.GetDirectoryName(((Microsoft.AspNetCore.Mvc.Rendering.ViewContext)urlHelper.ActionContext).ExecutingFilePath).Replace("\\", "/");
				string[] viewPathParts = viewPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

				// viewPathParts[0] will be "extensions", and viewPathParts[1] will be the extension folder name
				if (viewPathParts.Length > 2)
				{
					System.Uri extensionPath = urlHelper.GetAbsoluteUri($"{viewPathParts[0]}/{viewPathParts[1]}");
					return new System.Uri(extensionPath, ParseScriptPath(url, EXTENSIONPATH_TOKEN)).AbsolutePath;
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
	}
}
