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
		/// Convert the specified content to Html
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public static string ToHtml(this Content content)
		{
			return ToHtml(content.Value, content.ContentType);
		}

		/// <summary>
		/// Convert the specified content to Html
		/// </summary>
		/// <param name="content"></param>
		/// <param name="contentType"></param>
		/// <returns></returns>
		public static string ToHtml(string content, string contentType)
		{
			switch (contentType)
			{
				case "text/markdown":
					if (pipeline == null)
					{
						pipeline = new Markdig.MarkdownPipelineBuilder()
							.UseAdvancedExtensions()
							.UsePipeTables()
							.UseBootstrap()
							.Build();
					}
					return Markdown.ToHtml(content, pipeline);

				case "text/plain":					
					return content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br />");

				default:  // "text/html"
					return content;
			}

		}

	}
}
