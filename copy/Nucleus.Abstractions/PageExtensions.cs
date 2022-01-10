using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions
{
	public static class PageExtensions
	{
		public static PageRoute DefaultPageRoute(this Page page)
		{
			PageRoute result = page.Routes.Where(route => route.Id == page.DefaultPageRouteId).FirstOrDefault();

			if (result == null && page.Routes.Count > 0)
			{
				result = page.Routes[0];
			}

			return result;
		}

		public static IList<LayoutDefinition> InsertDefaultListItem(this IList<LayoutDefinition> list)
		{
			LayoutDefinition item = new();
			item.FriendlyName = "(site default)";

			list.Insert(0, item);
			return list;
		}

		public static IList<ContainerDefinition> InsertDefaultListItem(this IList<ContainerDefinition> list)
		{
			ContainerDefinition item = new();
			item.FriendlyName = "(default container)";

			list.Insert(0, item);
			return list;
		}
	}
}
