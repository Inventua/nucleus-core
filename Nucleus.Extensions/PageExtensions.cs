using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension functions for <seealso cref="Page"/>.
	/// </summary>
	public static class PageExtensions
	{
		/// <summary>
		/// Return the default page route, or if none are specified, the first route.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public static PageRoute DefaultPageRoute(this Page page)
		{
			PageRoute result = page.Routes.Where(route => route.Id == page.DefaultPageRouteId).FirstOrDefault();

      // if a default has not been set, use the first active one
      if (result == null && page.Routes.Count > 0)
      {
        result = page.Routes.Where(route => route.Type == PageRoute.PageRouteTypes.Active).FirstOrDefault();
      }

      // use any route if no active routes were found
      if (result == null && page.Routes.Count > 0)
			{
				result = page.Routes[0];
			}

			return result;
		}

		/// <summary>
		/// Return the layout path (relative path) for a page, defaulting to site settings as necessary.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="site"></param>
		/// <returns></returns>
		public static string LayoutPath(this Page page, Site site)
		{
			if (page.LayoutDefinition == null)
			{
				if (site.DefaultLayoutDefinition == null)
				{
					return $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.LAYOUTS_FOLDER}/{Nucleus.Abstractions.Managers.ILayoutManager.DEFAULT_LAYOUT}";
				}
				else
				{
					return site.DefaultLayoutDefinition.RelativePath;
				}
			}
			else
			{
				return page.LayoutDefinition.RelativePath;
			}
		}

		/// <summary>
		/// Insert a "default" layout selection.
		/// </summary>
		/// <param name="list"></param>
		/// <returns></returns>
		public static IList<LayoutDefinition> InsertDefaultListItem(this IList<LayoutDefinition> list)
		{
			LayoutDefinition item = new();
			item.FriendlyName = "(default)";

			list.Insert(0, item);
			return list;
		}

		/// <summary>
		/// Insert a "default" layout selection.
		/// </summary>
		public static IList<ContainerDefinition> InsertDefaultListItem(this IList<ContainerDefinition> list)
		{
			ContainerDefinition item = new();
			item.FriendlyName = "(default)";

			list.Insert(0, item);
			return list;
		}
	}
}
