﻿// Stylesheet and script merging has been removed.  See \Nucleus.Core\FileProviders\MergedFileProviderMiddleware for more information.
//using System;
//using System.Collections.Generic;
//using System.Text;
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

//namespace Nucleus.ViewFeatures.TagHelpers
//{
//	/// <summary>
//	/// Tag Helper used to merge multiple &lt;script&gt; tags into a single "merged" tag, which is handled by the MergedFileProvider.  A 
//	/// single &lt;MergeScripts&gt; can wrap all of your script elements.  
//	/// </summary>
//	/// <remarks>
//	/// The MergeScriptsTagHelper is implemented within the Nucleus shared _Layout Razor layout and wraps all javascript scripts,
//	/// including any scripts that you add by using <see cref="Nucleus.ViewFeatures.HtmlHelpers.AddScriptHtmlHelper.AddScript(IHtmlHelper, string)"/>.  
//	/// Under normal circumstances, you should not need to use the MergeScriptsTagHelper in your code.
//	/// <br/><br/>
//	/// There is no HtmlHelper equivalent for the MergeScriptsTagHelper.
//	/// </remarks>
//	/// <internal>
//	/// The merge scripts tag helper is for internal use.  It is not intended for use by Nucleus extensions.  Extensions should use the AddScript 
//	/// Html helper to add script tags to page output.  Scripts added with <see cref="Nucleus.ViewFeatures.HtmlHelpers.AddScriptHtmlHelper.AddScript(IHtmlHelper, string)"/> are 
//	/// automatically merged, if your site is configured to merge scripts.
//	/// </internal>
//	public class MergeScriptsTagHelper : TagHelper
//	{
//		private const char SEPARATOR_CHAR = ',';

//		private ILogger<MergeScriptsTagHelper> Logger { get; }

//		private ResourceFileOptions Options { get; }

//		/// <summary>
//		/// Provides access to view context.
//		/// </summary>
//		[ViewContext]
//		[HtmlAttributeNotBound]
//		public ViewContext ViewContext { get; set; }

//		/// <summary>
//		/// Constructor
//		/// </summary>
//		/// <param name="options"></param>
//		/// <param name="Logger"></param>
//		public MergeScriptsTagHelper(IOptions<ResourceFileOptions> options, ILogger<MergeScriptsTagHelper> Logger)
//		{
//			this.Options = options.Value;
//			this.Logger = Logger;
//		}

//		/// <summary>
//		/// Merge scripts
//		/// </summary>
//		/// <param name="context"></param>
//		/// <param name="output"></param>
//		/// <returns></returns>
//		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
//		{
//			TagHelperContent content = await output.GetChildContentAsync();
//			List<ScriptElement> scripts = new List<ScriptElement>();
//			TagBuilder builder = null;
//			string unmergedcontent = "";
//			string originalContent = content.GetContent();

//			if (!this.Options.MergeJs)
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
//									case "script":
//										ScriptElement script = new ScriptElement { Src = reader.GetAttribute("src"), Type = reader.GetAttribute("type") };
//										Boolean.TryParse(reader.GetAttribute("data-dynamic"), out bool isDynamic);

//										if (!isDynamic && script.Src.StartsWith("/"))  // only operate on scripts with a relative path
//										{
//											Logger.LogInformation("Merging [{0}] {1}", script.Type, script.Src);

//											if (!String.IsNullOrEmpty(script.Type) && script.Type != "text/javascript")
//											{
//												// one of the <script>s is not javascript, write child content out unmodified
//												WriteOriginalContent(output, originalContent);
//												return;
//											}
//											scripts.Add(script);
//										}
//										else
//										{
//											// Add script to the un-merged scripts list
//											Logger.LogInformation("Skipping [{0}] {1}", script.Type, script.Src);
//											unmergedcontent += script.ToString();
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

//			if (scripts.Count > 0)
//			{
//				string scriptUrl = "";
//				foreach (ScriptElement script in scripts)
//				{
//					if (!String.IsNullOrEmpty(scriptUrl))
//					{
//						scriptUrl += SEPARATOR_CHAR;
//					}
//					scriptUrl += script.Src;
//				}

//				IUrlHelper urlHelper = this.ViewContext.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(this.ViewContext);

//				builder = new TagBuilder("script");				
//				builder.Attributes.Add("src", urlHelper.Content($"~/merged.js?src={System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(scriptUrl))}{this.Version()}"));

//				Logger.LogInformation("Merged Uri {0}", builder.Attributes["src"]);
//			}

//			if (builder != null)
//			{
//				output.SuppressOutput();

//				if (!String.IsNullOrEmpty(unmergedcontent))
//				{
//					output.Content.AppendHtml(unmergedcontent);
//				}
//				output.Content.AppendHtml(builder);
//			}
//			else
//			{
//				// no <scripts> found, write out content unmodified
//				Logger.LogTrace("No <script> elements found");
//				WriteOriginalContent(output, originalContent);
//				return;
//			}
//		}

//		private string Version()
//		{
//			return "&v=" + this.GetType().Assembly.GetName().Version;
//		}
//		private void WriteOriginalContent(TagHelperOutput output, string originalContent)
//		{
//			output.SuppressOutput();
//			output.Content.AppendHtml(originalContent);
//		}

//		private class ScriptElement
//		{
//			public string Type { get; set; }
//			public string Src { get; set; }

//			public override string ToString()
//			{
//				if (String.IsNullOrEmpty(this.Type))
//				{
//					return $"<script src=\"{Src}\"></script>";
//				}
//				else
//				{
//					return $"<script type=\"{Type}\" src=\"{Src}\"></script>";
//				}
//			}
//		}

//	}
//}