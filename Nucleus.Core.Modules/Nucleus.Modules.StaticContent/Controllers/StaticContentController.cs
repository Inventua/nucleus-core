using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.ViewFeatures;
using Nucleus.Modules.StaticContent.Models;

namespace Nucleus.Modules.StaticContent.Controllers
{
	[Extension("StaticContent")]
	public class StaticContentController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IFileSystemManager FileSystemManager { get; }
		private ICacheManager CacheManager { get; }

		public StaticContentController(Context Context, ICacheManager cacheManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.CacheManager = cacheManager;

			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			RenderedContent renderedContent = null;
			string key;

			ViewModels.Viewer viewModel = await BuildViewModel();

			try
			{
				if (viewModel.DefaultFile != null && viewModel.DefaultFileId != Guid.Empty)
				{
					// Check that the user has permission to view the static file
					viewModel.DefaultFile.Parent.Permissions = await this.FileSystemManager.ListPermissions(viewModel.DefaultFile.Parent);

					if (!User.HasViewPermission(this.Context.Site, viewModel.DefaultFile.Parent))
					{
						viewModel.Content = null;
					}
					else
					{
						if (viewModel.DefaultFile.IsMarkdown() || viewModel.DefaultFile.IsText() || viewModel.DefaultFile.IsContent())
						{
							key = this.Context.Site.Id + ":" + viewModel.DefaultFile.Id;
							renderedContent = await this.CacheManager.StaticContentCache().GetAsync(key, async key =>
							{
								using (System.IO.Stream content = await this.FileSystemManager.GetFileContents(this.Context.Site, viewModel.DefaultFile))
								{
									if (viewModel.DefaultFile.IsMarkdown())
									{
										return new RenderedContent(ContentExtensions.ToHtml(GetStreamAsString(content), "text/markdown"));
									}
									else if (viewModel.DefaultFile.IsText())
									{
										return new RenderedContent(ContentExtensions.ToHtml(GetStreamAsString(content), "text/plain"));
									}
									else if (viewModel.DefaultFile.IsContent())
									{
										return new RenderedContent(GetStreamAsString(content));
									}
								}
								return new RenderedContent("");
							});
						}
						else
						{
							// Redirect to the file.
							if (viewModel.DefaultFile.Capabilities.CanDirectLink)
							{
								System.Uri uri = await this.FileSystemManager.GetFileDirectUrl(this.Context.Site, viewModel.DefaultFile);
								if (uri != null)
								{
									return new RedirectResult(uri.AbsoluteUri, true);
								}
								else
								{
									return NotFound();
								}
							}
							else
							{
								return new RedirectResult("~/" + Url.FileLink(viewModel.DefaultFile), true);
							}
						}

					}
				}

				if (!String.IsNullOrEmpty(renderedContent?.Content))
				{   
					// Parse the output (html) for static file links
					HtmlAgilityPack.HtmlDocument document = new();
					document.LoadHtml(renderedContent.Content);

					await ReplaceAttribute(viewModel, document, "img", "src");
					await ReplaceAttribute(viewModel, document, "link", "href");
					await ReplaceAttribute(viewModel, document, "script", "src");

					viewModel.Content = new(document.DocumentNode.OuterHtml);
				}

				return View("Viewer", viewModel);
			}
			catch (System.IO.FileNotFoundException)
			{
				return NotFound();
			}

		}

		private async Task ReplaceAttribute(ViewModels.Viewer viewModel, HtmlAgilityPack.HtmlDocument document, string nodeType, string attributeName)
		{
			HtmlAgilityPack.HtmlNodeCollection nodes = document.DocumentNode.SelectNodes($"//{nodeType.ToLower()}");

			if (nodes != null)
			{

				foreach (HtmlAgilityPack.HtmlNode node in nodes)
				{
					string attributeValue = node.GetAttributeValue(attributeName, "");
					if (attributeValue != null)
					{
						if (!attributeValue.Contains(System.Uri.SchemeDelimiter))
						{
							string localPath;
							string query;

							int position = attributeValue.IndexOfAny(new char[] { '?', '#' });
							if (position < 0)
							{
								localPath = attributeValue;
								query = "";
							}
							else
							{
								localPath = attributeValue.Substring(0, position);
								query = attributeValue.Substring(position);
							}

							// attribute is a relative path
							try
							{
								File file = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.DefaultFile.Provider, viewModel.DefaultFile.Parent.Path + (String.IsNullOrEmpty(viewModel.DefaultFile.Parent.Path) ? "" : "/") + localPath);

								if (file != null && file.Capabilities.CanDirectLink)
								{
									// replace attribute with direct file link
									string fileUri = (await this.FileSystemManager.GetFileDirectUrl(this.Context.Site, file)).ToString();
									if (fileUri.Contains('?') && query.StartsWith('?'))
									{
										query = $"&{query.Substring(1)}";
									}
									node.SetAttributeValue(attributeName, fileUri + query);
								}
								else
								{
									string fileUri = Url.FileLink(file);
									node.SetAttributeValue(attributeName, fileUri + query);
								}

								// For images, if we know the height/width of the image file, and if a height/width attribute is not already 
								// specifed, add the attribute(s).
								if (node.Name.Equals( "img" , StringComparison.OrdinalIgnoreCase ))
								{
									if (file.Width.HasValue && String.IsNullOrEmpty(node.GetAttributeValue("width", "")))
									{
										node.SetAttributeValue("width", file.Width.Value.ToString());
									}
									if (file.Height.HasValue && String.IsNullOrEmpty(node.GetAttributeValue("height", "")))
									{
										node.SetAttributeValue("height", file.Height.Value.ToString());
									}
								}
							}
							catch (System.IO.FileNotFoundException)
							{

							}

						}
					}
				}
			}
		}

		private string GetStreamAsString(System.IO.Stream stream)
		{
			using (System.IO.StreamReader reader = new(stream))
			{
				return reader.ReadToEnd();
			}
		}

		private async Task<ViewModels.Viewer> BuildViewModel()
		{
			ViewModels.Viewer viewModel = new();

			viewModel.ReadSettings(this.Context.Module);

			try
			{
				if (viewModel.DefaultFileId != Guid.Empty)
				{
					viewModel.DefaultFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.DefaultFileId);
				}
				else
				{
					viewModel.DefaultFile = null;
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				viewModel.DefaultFile = null;
			}

			return viewModel;
		}
	}
}