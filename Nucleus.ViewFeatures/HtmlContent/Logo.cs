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
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders a logo control.
	/// </summary>
	/// <remarks>
	/// The logo control renders an image for the site's selected logo.  If there is no logo configured for the site, nothing is rendered.
	/// This function is used by the <see cref="HtmlHelpers.LogoHtmlHelper"/> and <see cref="TagHelpers.LogoTagHelper"/>.
	/// </remarks>
	/// <internal />
	/// <hidden />
	internal static class Logo
	{
		internal static async Task<TagBuilder> Build(ViewContext context, string caption, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			Site site = context.HttpContext.RequestServices.GetService<Context>().Site;
			IFileSystemManager fileSystemManager = context.HttpContext.RequestServices.GetService<IFileSystemManager>();
			
			if (site.SiteSettings.TryGetValue(Site.SiteFilesKeys.LOGO_FILEID, out Guid fileId))
			{
				File logoFile = null;

				if (fileId != Guid.Empty)
				{
					try
					{
						logoFile = await fileSystemManager.GetFile(site, fileId);
					}
					catch (Exception)
					{
						logoFile = null;
					}
				}

				if (logoFile != null)
				{
					string linkTitle = String.IsNullOrEmpty(caption) ? site.Name : caption;
					TagBuilder outputBuilder = new("a");
					outputBuilder.Attributes.Add("href", urlHelper.Content("~/"));
					outputBuilder.Attributes.Add("title", linkTitle);
					
					TagBuilder imageBuilder = new("img");

					// render a direct link if the file system provider supports it (because it is faster than returning a redirect to azure storage).  This "skips"
					// the Nucleus permissions check, but the performance difference is > 200ms.
					if (logoFile.Capabilities.CanDirectLink)
					{
						System.Uri uri = await fileSystemManager.GetFileDirectUrl(site, logoFile);
						if (uri != null)
						{
							imageBuilder.Attributes.Add("src", uri.AbsoluteUri);
						}
						else
						{
							imageBuilder.Attributes.Add("src", urlHelper.FileLink(logoFile));
						}
					}
					else
					{
						imageBuilder.Attributes.Add("src", urlHelper.FileLink(logoFile));
					}

					if (logoFile.Width.HasValue)
					{
						imageBuilder.Attributes.Add("width", logoFile.Width.Value.ToString());
					}
					if (logoFile.Height.HasValue)
					{
						imageBuilder.Attributes.Add("height", logoFile.Height.Value.ToString());
					}

					imageBuilder.Attributes.Add("alt", linkTitle);
					imageBuilder.TagRenderMode = TagRenderMode.SelfClosing;

					outputBuilder.InnerHtml.AppendHtml(imageBuilder);
					outputBuilder.MergeAttributes(htmlAttributes);

					return outputBuilder;
				}
			}	
			
			return null;						
		}		
	}
}
