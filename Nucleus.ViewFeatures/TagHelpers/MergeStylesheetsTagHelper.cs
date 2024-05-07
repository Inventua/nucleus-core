// Stylesheet and script merging has been removed.  See \Nucleus.Core\FileProviders\MergedFileProviderMiddleware for more information.
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.AspNetCore.Razor.TagHelpers;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using System.IO;
//using Microsoft.Extensions.Logging;
//using Nucleus.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.DependencyInjection;
//using Nucleus.Abstractions.Models.Configuration;
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
//using Microsoft.AspNetCore.Mvc.Routing;
//using Microsoft.AspNetCore.Html;
//using Microsoft.AspNetCore.Routing;
//using Nucleus.Abstractions;

//namespace Nucleus.ViewFeatures.TagHelpers
//{
//	/// <summary>
//	/// Tag Helper used to merge multiple CSS stylesheet &lt;link&gt; tags into a single "merged" tag, which is handled by the MergedFileProvider.  
//	/// A single &lt;MergeLinks&gt; can wrap all of your link elements.  The MergeLinksTagHelper will merge &lt;link&gt; tags with the same 
//	/// path - it will generate separate &lt;link&gt; tags for each path in order to preserve relative paths within your CSS files.
//	/// </summary>
//	/// <remarks>
//	/// The MergeScriptsTagHelper is implemented within the Nucleus shared _Layout Razor layout and wraps all scripts,
//	/// including any scripts that you add with <see cref="Nucleus.ViewFeatures.HtmlHelpers.AddStyleHtmlHelper.AddStyle(IHtmlHelper, string)"/>.  
//	/// Under normal circumstances, you should not need to use the MergeStylesheetsTagHelper in your code.
//	/// <br/><br/>
//	/// There is no HtmlHelper equivalent for the MergeStylesheetsTagHelper.
//	/// </remarks>
//	/// <internal>
//	/// The merge stylesheets tag helper is for internal use.  It is not intended for use by Nucleus extensions.  Extensions should use the AddStyle 
//	/// Html helper to add stylesheets to page output.  Scripts added with <see cref="Nucleus.ViewFeatures.HtmlHelpers.AddStyleHtmlHelper.AddStyle(IHtmlHelper, string)"/> are 
//	/// automatically merged, if your site is configured to merge stylesheets.
//	/// </internal>
//	public class MergeStylesheetsTagHelper : TagHelper
//	{
//		private const char SEPARATOR_CHAR = ',';
//		private ILogger<MergeStylesheetsTagHelper> Logger { get; }
//		private ResourceFileOptions Options { get; }

//		/// <summary>
//		/// Constructor
//		/// </summary>
//		/// <param name="options"></param>
//		/// <param name="Logger"></param>
//		public MergeStylesheetsTagHelper(IOptions<ResourceFileOptions> options, ILogger<MergeStylesheetsTagHelper> Logger)
//		{
//			this.Options = options.Value;
//			this.Logger = Logger;
//		}

//		/// <summary>
//		/// Provides access to view context.
//		/// </summary>
//		[ViewContext]
//		[HtmlAttributeNotBound]
//		public ViewContext ViewContext { get; set; }

//		/// <summary>
//		/// Merge style sheets.
//		/// </summary>
//		/// <param name="context"></param>
//		/// <param name="output"></param>
//		/// <returns></returns>
//		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
//		{
//			TagHelperContent content = await output.GetChildContentAsync();
//			Dictionary<string, List<LinkElement>> links = new Dictionary<string, List<LinkElement>>();
//			HtmlContentBuilder builder = null;
//			TagBuilder linkbuilder = null;
//			string unmergedcontent = "";
//			string originalContent = content.GetContent();
//			string path;

//			if (!this.Options.MergeCss)
//			{
//				WriteOriginalContent(output, originalContent);
//				return;
//			}

//			using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(originalContent)), new System.Xml.XmlReaderSettings { Async = true, ConformanceLevel = System.Xml.ConformanceLevel.Fragment }))
//			{
//				while (await reader.ReadAsync())
//				{
//					switch (reader.NodeType)
//					{
//						case System.Xml.XmlNodeType.Element:
//							{
//								switch (reader.Name)
//								{
//									case "link":
//										LinkElement link = new LinkElement { Href = reader.GetAttribute("href"), Rel = reader.GetAttribute("rel") };
//										//string isDynamic = reader.GetAttribute("data-dynamic");

//										Boolean.TryParse(reader.GetAttribute("data-dynamic"), out bool isDynamic);

//										if (!isDynamic && link.Href.StartsWith("/"))  // only operate on scripts with a relative path
//										{
//											Logger.LogInformation("Merging [{0}] {1}", link.Rel, link.Href);

//											if (link.Rel != "stylesheet")
//											{
//												// one of the links is not a CSS stylesheet, write child content out unmodified
//												WriteOriginalContent(output, originalContent);
//												return;
//											}

//											path = System.IO.Path.GetDirectoryName(link.Href).ToLower().Replace('\\', '/');
//											if (path.StartsWith("/"))
//											{
//												path = path.Substring(1);
//											}

//											if (!links.ContainsKey(path))
//											{
//												links.Add(path, new List<LinkElement>());
//											}
//											links[path].Add(link);
//										}
//										else
//										{
//											Logger.LogInformation("Skipping [{0}] {1}", link.Rel, link.Href);

//											// Add script to the un-merged links list
//											unmergedcontent += link.ToString();
//										}
//										break;

//									default:
//										// taghelper contains something else, write child content out unmodified
//										Logger.LogInformation("Found unexpected element {0}, merge skipped", reader.Name);
//										WriteOriginalContent(output, originalContent);
//										return;
//								}
//							}
//							break;
//					}
//				}
//			}

//			if (links.Count > 0)
//			{
//				foreach (string linkpath in links.Keys)
//				{
//					string linksUrl = "";

//					if (links[linkpath].Count == 1)
//					{
//						// Only one css file in path, write original
//						linkbuilder = new TagBuilder("link");
//						linkbuilder.TagRenderMode = TagRenderMode.SelfClosing;

//						linkbuilder.Attributes.Add("rel", "stylesheet");
//						linkbuilder.Attributes.Add("href", links[linkpath].First().Href);

//						if (builder == null)
//						{
//							builder = new HtmlContentBuilder();
//						}
//						builder.AppendHtml(linkbuilder);
//					}
//					else
//					{
//						foreach (LinkElement link in links[linkpath])
//						{
//							if (!String.IsNullOrEmpty(linksUrl))
//							{
//								linksUrl += SEPARATOR_CHAR;
//							}
//							linksUrl += link.Href;
//						}

//						IUrlHelper urlHelper = this.ViewContext.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(this.ViewContext);

//						linkbuilder = new TagBuilder("link");
//						linkbuilder.TagRenderMode = TagRenderMode.SelfClosing;

//						linkbuilder.Attributes.Add("rel", "stylesheet");
//						linkbuilder.Attributes.Add("href", $"/{linkpath}/merged.css?src={System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(linksUrl))}{this.Version()}");

//						if (builder == null)
//						{
//							builder = new HtmlContentBuilder();
//						}
//						builder.AppendHtml(linkbuilder);

//						Logger.LogInformation("Merged Uri {0}", linkbuilder.Attributes["href"]);
//					}
//				}
//			}

//			if (builder != null)
//			{
//				output.SuppressOutput();
//				output.Content.AppendHtml(builder);
//				if (!String.IsNullOrEmpty(unmergedcontent))
//				{
//					output.Content.AppendHtml(unmergedcontent);
//				}
//			}
//			else
//			{
//				// no <link>s found, write out content unmodified
//				Logger.LogTrace("No <link> elements found");
//				WriteOriginalContent(output, originalContent);
//				return;
//			}
//		}

//		private string Version()
//		{
//			return "&v=" + this.GetType().Assembly.GetName().Version;
//		}

//		private void WriteOriginalContent(TagHelperOutput Output, string OriginalContent)
//		{
//			Output.SuppressOutput();
//			Output.Content.AppendHtml(OriginalContent);
//		}

//		private class LinkElement
//		{
//			public string Rel { get; set; }
//			public string Href { get; set; }
//			public override string ToString()
//			{
//				return $"<link rel=\"{Rel}\" href=\"{Href}\"></link>";
//			}
//		}

//	}
//}