//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc.Razor;
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace Nucleus.Extensions.Plugins
//{
//	// https://www.c-sharpcorner.com/article/expanding-razor-view-location-and-sub-areas-in-asp-net-core/
//	public class ModuleViewLocationExpander : IViewLocationExpander
//	{
//		private string Path { get; }

//		public ModuleViewLocationExpander(string path)
//		{
//			this.Path = path;
//		}

//		public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
//		{
//			return ExpandModuleHierarchy(context,viewLocations);
//		}

//		private string FormatPath(string path)
//		{
//			//return path;
//			return path.Replace(this.Path, "", StringComparison.OrdinalIgnoreCase).Replace('\\', '/');
//		}

//		private IEnumerable<string> ExpandModuleHierarchy(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
//    {
//			//List<string> results = new List<string>();



//			foreach (string folder in System.IO.Directory.GetDirectories(System.IO.Path.Combine(this.Path, Nucleus.Abstractions.Folders.MODULES_FOLDER)))
//			{
//				string pagesPath = System.IO.Path.Combine(folder, "Pages");
//				string viewsPath = System.IO.Path.Combine(folder, "Views");
//				string sharedViewsPath = System.IO.Path.Combine(viewsPath, "Shared");
				
//				if (System.IO.Directory.Exists(pagesPath))
//				{
//					yield return FormatPath(System.IO.Path.Combine(pagesPath, $"{{1}}/{{0}}{RazorViewEngine.ViewExtension}"));
//					//results.Add(FormatPath(System.IO.Path.Combine(pagesPath, $"{{1}}/{{0}}{RazorViewEngine.ViewExtension}")));
//				}

//				if (System.IO.Directory.Exists(viewsPath))
//				{
//					yield return FormatPath(System.IO.Path.Combine(viewsPath, $"{{0}}{RazorViewEngine.ViewExtension}"));
//					//results.Add(FormatPath(System.IO.Path.Combine(viewsPath, $"{{0}}{RazorViewEngine.ViewExtension}")));
//				}

//				if (System.IO.Directory.Exists(sharedViewsPath))
//				{
//					yield return FormatPath(System.IO.Path.Combine(sharedViewsPath, $"{{1}}/{{0}}{RazorViewEngine.ViewExtension}"));
//					//results.Add(FormatPath(System.IO.Path.Combine(sharedViewsPath, $"{{1}}/{{0}}{RazorViewEngine.ViewExtension}")));
//				}
//			}

//			//results.AddRange(viewLocations);
//			//return results;
//		}

//    public void PopulateValues(ViewLocationExpanderContext context)
//		{
//			// The results from this implementation do not change per-request, so no implementation is required here			
//		}
//	}
//}
