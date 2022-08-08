using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.Modules.Search
{
	/// <summary>
	/// Renders a search control.
	/// </summary>	
	/// <remarks>
	/// This tag helper is intended for use by custom layouts.
	/// </remarks>
	[HtmlTargetElement("search")]
	public class SearchTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// The inner text of the anchor element.
		/// </summary>
		public Nucleus.Modules.Search.ViewModels.Settings.DisplayModes DisplayMode { get; set; } = ViewModels.Settings.DisplayModes.Full;

		/// <summary>
		/// The relative url of the results page.  This must match one of the routes for a page on your site.
		/// </summary>
		public string ResultsPageUrl { get; set; }

		private IHtmlHelper HtmlHelper { get; }

		public SearchTagHelper(IHtmlHelper htmlHelper)
		{
			this.HtmlHelper= htmlHelper;
		}

		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			output.SuppressOutput();
			(this.HtmlHelper as IViewContextAware).Contextualize(this.ViewContext);

			IHtmlContent searchControloutput = await SearchRenderer.Build(this.HtmlHelper, this.ResultsPageUrl, this.DisplayMode, null);
			if (searchControloutput != null)
			{
				searchControloutput.WriteTo(this.ViewContext.Writer, System.Text.Encodings.Web.HtmlEncoder.Default);
			}
		}
	}
}