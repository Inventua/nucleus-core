using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.XmlDocumentation.Models;
using Nucleus.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Extensions;

namespace Nucleus.XmlDocumentation
{
	public static class UrlExtensions
	{
		public static string GenerateUrl(this ApiClass apiClass, Page page, ApiDocument document)
		{
			return PageLink(page, $"{document.SourceFileName}/{apiClass.ControlId()}/");
		}

		public static string GenerateUrl(this ApiDocument document, Page page)
		{
			return PageLink(page, $"{@document.SourceFileName}/#{document.Namespace.MenuId()}");
		}

		private static string PageLink(Page page, string relativePath)
		{
			if (page == null) throw new ArgumentNullException(nameof(page));

			if (relativePath.StartsWith("/"))
			{
				relativePath = relativePath[1..];
			}

			if (page.Disabled || page.DefaultPageRoute() == null) return "";
			string path = page.DefaultPageRoute().Path;

			return path + (path.EndsWith("/") ? "" : "/") + relativePath + "/";
		}
	}
}
