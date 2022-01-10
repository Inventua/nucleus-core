//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.AspNetCore.Html;
//using System.Linq.Expressions;
//using Microsoft.AspNetCore.Mvc.ModelBinding;
//using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

//namespace Nucleus.ViewFeatures.HtmlHelpers
//{
//	public static class SettingsControlHtmlHelper
//	{
//		public static SettingsControl BeginSettingsControl(this IHtmlHelper html, string caption, string helptext, Nucleus.ViewFeatures.TagHelpers.SettingsControlTagHelper.RenderModes renderMode, object htmlAttributes = null)
//		{
//			return new SettingsControl(html, caption, helptext, renderMode, htmlAttributes);
//			//return Nucleus.ViewFeatures.HtmlContent.SettingsControl.Build(html.ViewContext, html., caption, helptext, renderMode, htmlAttributes);
//		}

//		/// <summary>
//		/// A class which renders the &lt;div class="DialogPanel"&gt; element start and end tags.
//		/// </summary>
//		/// <remarks>
//		/// This class is used by the DialogPanelHtmlHelper and is not intended for direct use by your application.
//		/// </remarks>
//		public class SettingsControl : ComplexHtmlHelperBase
//		{
//			private string Caption { get; }
//			private string HelpText { get; }
//			private Nucleus.ViewFeatures.TagHelpers.SettingsControlTagHelper.RenderModes RenderMode { get; }
//			private object HtmlAttributes { get; }

//			private IHtmlHelper HtmlHelper { get; }

//			internal SettingsControl(IHtmlHelper html, string caption, string helptext, Nucleus.ViewFeatures.TagHelpers.SettingsControlTagHelper.RenderModes renderMode, object htmlAttributes = null) : base(html, "DIV")
//			{
//				this.HtmlHelper = html;

//				this.Caption = caption;
//				this.HelpText = helptext;
//				this.RenderMode = renderMode;

//				this.HtmlAttributes = HtmlAttributes;

//				base.BeginRender();
//			}

//			internal override TagBuilder BuildContent()
//			{
//				return HtmlContent.SettingsControl.Build(this.HtmlHelper.ViewContext, this.Caption, this.HelpText, this.RenderMode, this.HtmlAttributes);
//			}


//		}
//	}
//}
