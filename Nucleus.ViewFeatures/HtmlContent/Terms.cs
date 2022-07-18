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
	/// Renders an site terms control.
	/// </summary>
	/// <remarks>
	/// The terms control renders a link to the site's terms of use page.  If there is no terms page configured for the site, nothing is rendered.
	/// This function is used by the <see cref="HtmlHelpers.TermsHtmlHelper"/> and <see cref="TagHelpers.TermsTagHelper"/>.
	/// </remarks>
	/// <internal />
	/// <hidden />
	internal static class Terms
	{
		internal static async Task<TagBuilder> Build(ViewContext context, string caption, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);

			SitePages sitePage = context.HttpContext.RequestServices.GetService<Context>().Site.GetSitePages();
			PageRoute TermsPageRoute = null;
			if (sitePage.TermsPageId.HasValue)
			{
				IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();
				Page TermsPage = await pageManager.Get(sitePage.TermsPageId.Value);
				if (TermsPage != null)
				{
					TermsPageRoute = TermsPage.DefaultPageRoute();
				}
			}

			if (TermsPageRoute == null)
			{
				return null;
			}
			else
			{
				TagBuilder outputBuilder = new("a");

				outputBuilder.Attributes.Add("href", urlHelper.Content("~" + TermsPageRoute.Path));
				outputBuilder.InnerHtml.SetContent(caption ?? "Terms");
				outputBuilder.MergeAttributes(htmlAttributes);

				return outputBuilder;
			}
		}		
	}
}
