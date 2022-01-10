using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;

namespace Nucleus.ViewFeatures
{
	/// <summary>
	/// The ComplexHtmlHelperBase abstract class is used used internally to provide shared rendering functionality for complex Html Helpers.
	/// </summary>
	public abstract class ComplexHtmlHelperBase : IDisposable
	{
		internal IHtmlHelper Helper { get; }
		private string TagName { get; }

		/// <summary>
		/// ComplexHtmlHelperBase constructor.
		/// </summary>
		/// <param name="html">The IHtmlHelper instance this method extends.</param>
		/// <param name="tagname">The outer tag name being rendered by the inheriting class.</param>
		public ComplexHtmlHelperBase(IHtmlHelper html, string tagname)
		{
			this.Helper = html;
			this.TagName = tagname;
		}

		internal void BeginRender()
		{
			TextWriter writer = this.Helper.ViewContext.Writer;
			TagBuilder builder = BuildContent();

			builder.TagRenderMode = TagRenderMode.StartTag;
			builder.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
			builder.InnerHtml.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
		}

		internal abstract TagBuilder BuildContent();

		/// <summary>
		/// Implement IDisposable 
		/// </summary>
		public virtual void Dispose()
		{
			TextWriter writer = this.Helper.ViewContext.Writer;
			TagBuilder builder = new(this.TagName)
			{
				TagRenderMode = TagRenderMode.EndTag
			};
			builder.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);

			GC.SuppressFinalize(this);
		}



	}
}
