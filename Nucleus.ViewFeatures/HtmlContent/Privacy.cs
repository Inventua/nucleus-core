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
using Nucleus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders an privacy control.
	/// </summary>
	/// <remarks>
	/// The privacy control renders a link to the site's privacy page.  If there is no privacy page configured for the site, nothing is rendered.
	/// This function is used by the <see cref="HtmlHelpers.PrivacyHtmlHelper"/> and <see cref="TagHelpers.PrivacyTagHelper"/>.
	/// </remarks>
	internal static class Privacy
	{
		internal static async Task<TagBuilder> Build(ViewContext context, string caption, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);

			SitePages sitePage = context.HttpContext.RequestServices.GetService<Context>().Site.GetSitePages();
			PageRoute privacyPageRoute = null;
			if (sitePage.PrivacyPageId.HasValue)
			{
				IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();
				Page privacyPage = await pageManager.Get(sitePage.PrivacyPageId.Value);
				if (privacyPage != null)
				{
					privacyPageRoute = privacyPage.DefaultPageRoute();
				}
			}

			if (privacyPageRoute == null)
			{
				return null;
			}
			else
			{
				TagBuilder outputBuilder = new("a");

				outputBuilder.Attributes.Add("href", urlHelper.Content("~" + privacyPageRoute.Path));
				outputBuilder.InnerHtml.SetContent(caption ?? "Privacy");
				outputBuilder.MergeAttributes(htmlAttributes);

				return outputBuilder;
			}
		}		
	}
}
