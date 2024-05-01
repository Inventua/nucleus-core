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
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.Plugins
{
	public class ExtensionViewLocationExpander : IViewLocationExpander
	{
		/// <summary>
		/// If the request is for an assembly located in the extensions folder, return view location patterns for extensions.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="viewLocations"></param>
		/// <returns></returns>
		public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
		{
			if (context.ActionContext is ControllerContext actionContext)
			{
        // we need to use the extension folder in the returned view paths (a sub-folder in /Extensions), not the extension
        // name, which may be different.
        string assemblyLocation = actionContext.ActionDescriptor.ControllerTypeInfo.Assembly.Location;
				string extensionFolder = AssemblyLoader.GetExtensionFolderName(assemblyLocation);

        if (!String.IsNullOrEmpty(extensionFolder))
        {
          actionContext.RouteData.Values.TryGetValue("area", out object area);
          return GetViewPaths(extensionFolder, area);
        }
      }
      
			return viewLocations;
		}

    /// <summary>
    /// Return view paths for the specified extension.
    /// </summary>
    /// <param name="extensionFolder"></param>
    /// <param name="area"></param>
    /// <returns></returns>
    private IEnumerable<string> GetViewPaths(string extensionFolder, object area)
    {
      //  The view engine path string tokens are:
      //  {0} view name
      //  {1} controller name
      //  {2} area name (if present)

      // the most likely view path(s) should be returned first.
      yield return $"/{FolderOptions.EXTENSIONS_FOLDER}/{extensionFolder}/Views/{{0}}.cshtml";
      yield return $"/{FolderOptions.EXTENSIONS_FOLDER}/{extensionFolder}/Views/Shared/{{0}}.cshtml";
      yield return $"/{FolderOptions.EXTENSIONS_FOLDER}/{extensionFolder}/Views/{{1}}/{{0}}.cshtml";
      yield return $"/{FolderOptions.EXTENSIONS_FOLDER}/{extensionFolder}/Pages/{{0}}.cshtml";

      if (area != null)
      {
        yield return $"/{FolderOptions.EXTENSIONS_FOLDER}/{extensionFolder}/Areas/{{2}}/Views/{{1}}/{{0}}.cshtml";
        yield return $"/{FolderOptions.EXTENSIONS_FOLDER}/{extensionFolder}/Areas/{{2}}/Views/Shared/{{0}}.cshtml";
      }
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
			if (context.ActionContext is ControllerContext actionContext)
			{
				string assemblyLocation = actionContext.ActionDescriptor.ControllerTypeInfo.Assembly.Location;
				string extensionFolder = AssemblyLoader.GetExtensionFolderName(assemblyLocation);

				if (!String.IsNullOrEmpty(extensionFolder))
				{
          if (actionContext.RouteData.Values.TryGetValue("area", out object area))
					{
						context.Values["area"] = (string)area;
					}

					context.Values["extension"] = extensionFolder;
				}
			}
		}
	}
}
