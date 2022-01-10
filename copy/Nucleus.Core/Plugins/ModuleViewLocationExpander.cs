using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions;

namespace Nucleus.Core.Plugins
{
	public class ModuleViewLocationExpander : IViewLocationExpander
	{
		/// <summary>
		/// If the request is for an assembly located in the extensions folder, return view location patterns which point to the relevant
		/// extensions's views folder or Areas/[area]/[controller]/Views folder.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="viewLocations"></param>
		/// <returns></returns>
		public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
		{
			ControllerContext actionContext = context.ActionContext as ControllerContext;

			if (actionContext != null)
			{
				string assemblyLocation = actionContext.ActionDescriptor.ControllerTypeInfo.Assembly.Location;
				string extensionFolder = AssemblyLoader.GetExtensionFolderName(assemblyLocation);

				if (!String.IsNullOrEmpty(extensionFolder))
				{
					// Assembly was loaded from an extension folder, return view paths for that extension
					if (actionContext.RouteData.Values.ContainsKey("area") && !String.IsNullOrEmpty((string)actionContext.RouteData.Values["area"]))
					{
						// controller is in an [area], return /Extensions/[extension]/Areas/[area]/[controller]/Views/[view].cshtml
						return new List<string>()
						{
							$"/Extensions/{extensionFolder}/Areas/{actionContext.RouteData.Values["area"]}/Views/{{1}}/{{0}}.cshtml"
						};
					}
					else
					{
						// controller is not in an [area], return /Extensions/[extension]/Views/[view].cshtml
						return new List<string>()
						{
							$"/Extensions/{extensionFolder}/Views/{{0}}.cshtml",
							$"/Extensions/{extensionFolder}/Pages/{{0}}.cshtml"
						};
					}
				}
			}

			return viewLocations;
		}

		/// <summary>
		/// Populate the context.Values dictionary with values that can change the result of ExpandViewLocations()
		/// </summary>
		/// <param name="context"></param>
		/// <remarks>
		/// NET core uses the context.Values dictionary as a key to its cache of view locations.
		/// </remarks>
		public void PopulateValues(ViewLocationExpanderContext context)
		{
			ControllerContext actionContext = context.ActionContext as ControllerContext;

			if (actionContext != null)
			{
				string assemblyLocation = actionContext.ActionDescriptor.ControllerTypeInfo.Assembly.Location;
				string extensionFolder = AssemblyLoader.GetExtensionFolderName(assemblyLocation);

				if (!String.IsNullOrEmpty(extensionFolder))
				{
					if (actionContext.RouteData.Values.ContainsKey("area"))
					{
						context.Values["area"] = (string)actionContext.RouteData.Values["area"];
					}

					context.Values["extension"] = extensionFolder;
				}
			}
		}
	}
}
