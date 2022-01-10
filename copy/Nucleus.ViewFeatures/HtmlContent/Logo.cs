using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders a logo control.
	/// </summary>
	/// <remarks>
	/// The logo control renders an image for the site's selected logo.  If there is no logo configured for the site, nothing is rendered.
	/// This function is used by the <see cref="HtmlHelpers.LogoHtmlHelper"/> and <see cref="TagHelpers.LogoTagHelper"/>.
	/// </remarks>
	public static class Logo
	{
		internal static TagBuilder Build(ViewContext context, string caption, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			Site site = context.HttpContext.RequestServices.GetService<Context>().Site;
			FileSystemManager fileSystemManager = context.HttpContext.RequestServices.GetService<FileSystemManager>();
			site.SiteSettings.TryGetValue(Site.SiteImageKeys.LOGO_FILEID, out string fileIdvalue);
			
			if (Guid.TryParse(fileIdvalue, out Guid fileId))
			{
				File logoFile = fileSystemManager.GetFile(site, fileId);
				if (logoFile != null)
				{
					TagBuilder outputBuilder = new TagBuilder("a");
					outputBuilder.Attributes.Add("href", urlHelper.Content("~"));

					TagBuilder imageBuilder = new TagBuilder("img");
					imageBuilder.Attributes.Add("src", urlHelper.DownloadLink(logoFile));
					imageBuilder.Attributes.Add("alt", String.IsNullOrEmpty(caption) ? site.Name : caption);

					outputBuilder.InnerHtml.AppendHtml(imageBuilder);
					outputBuilder.MergeAttributes(htmlAttributes);

					return outputBuilder;
				}
			}	
			
			return null;						
		}		
	}
}
