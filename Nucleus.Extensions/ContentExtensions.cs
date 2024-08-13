using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Content extensions.
	/// </summary>
	public static class ContentExtensions
	{
	  private static MarkdownPipeline pipeline;

		/// <summary>
		/// Converts the specified content to Html.
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public static string ToHtml(this Content content)
		{
			return ToHtml(content.Value, content.ContentType);
		}

		/// <summary>
		/// Converts the specified string content to Html.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="contentType">MIME type of the original content.</param>
		/// <returns></returns>
		/// <remarks>
		/// The contentType can be text/markdown or text/plain.  All other content type values are treated as text/html and
		/// are not converted.
		/// </remarks>
		public static string ToHtml(this string content, string contentType)
		{
      if (String.IsNullOrEmpty(content)) return "";

			switch (contentType)
			{
				case "text/markdown":
					if (pipeline == null)
					{
						pipeline = new Markdig.MarkdownPipelineBuilder()
							.UseAdvancedExtensions()
							.UsePipeTables()
							.UseAutoIdentifiers()
							.UseGridTables()
							.UseBootstrap()
							.UseGenericAttributes()
							.Build();
					}
					return Markdown.ToHtml(content, pipeline);

				case "text/plain":
          // replace double-line feeds with two <br> elements so that paragraphs work. Single line feeds get removed as they are
          // interpreted as being for ease of editing rather than intended for display.
					return content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n\n", "<br /><br />").Replace("\n", "");

				default:  // "text/html"
					return content;
			}

		}

	}
}
