using Microsoft.AspNetCore.Mvc;
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

namespace Nucleus.Modules.StaticContent.Controllers
{
	[Extension("StaticContent")]
	public class StaticContentController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IFileSystemManager FileSystemManager { get; }

		public StaticContentController(Context Context, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			try
			{
				ViewModels.Viewer viewModel = await BuildViewModel();

				File file;
				if (!String.IsNullOrEmpty(this.Context.Parameters))
				{
					file = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SourceFolder.Provider, viewModel.SourceFolder.Path + (String.IsNullOrEmpty(viewModel.SourceFolder.Path) ? "" : "/") + this.Context.Parameters);
				}
				else
				{
					file = viewModel.DefaultFile;
				}

				if (file != null && file.Id != Guid.Empty)
				{
					// Check that the user has permission to view the static file
					file.Parent.Permissions = await this.FileSystemManager.ListPermissions(file.Parent);

					if (!User.HasViewPermission(this.Context.Site, file.Parent))
					{
						viewModel.Content = "";
					}
					else
					{
						using (System.IO.Stream content = this.FileSystemManager.GetFileContents(this.Context.Site, file))
						{
							if (file.IsMarkdown())
							{
								viewModel.Content = ContentExtensions.ToHtml(GetStreamAsString(content), "text/markdown");
							}
							else if (file.IsText())
							{
								viewModel.Content = ContentExtensions.ToHtml(GetStreamAsString(content), "text/plain");
							}
							else if (file.IsContent())
							{
								viewModel.Content = GetStreamAsString(content);
							}
							else
							{
								//System.IO.MemoryStream data = new();
								//content.Position = 0;
								//content.CopyTo(data);
								//return File(data.ToArray(), GetMimeType(file));
								// Redirect to the file.
								if (file.Capabilities.CanDirectLink)
								{
									System.Uri uri = this.FileSystemManager.GetFileDirectUrl(this.Context.Site, file);
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
									return new RedirectResult(Url.FileLink(file), true);
								}
							}
						}
					}
				}
				else
				{
					return NotFound();
				}

				// Parse the output (html) for static file links
				HtmlAgilityPack.HtmlDocument document = new();
				document.LoadHtml(viewModel.Content);

				await ReplaceAttribute(viewModel, document, "img", "src");
				await ReplaceAttribute(viewModel, document, "link", "href");
				await ReplaceAttribute(viewModel, document, "script", "src");

				viewModel.Content = document.DocumentNode.OuterHtml;

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
							File file = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SourceFolder.Provider, viewModel.SourceFolder.Path + (String.IsNullOrEmpty(viewModel.SourceFolder.Path) ? "" : "/") + localPath);
							if (file != null && file.Capabilities.CanDirectLink)
							{
								// replace attribute with direct file link
								string fileUri = this.FileSystemManager.GetFileDirectUrl(this.Context.Site, file).ToString();
								if (fileUri.Contains('?') && query.StartsWith('?'))
								{
									query = $"&{query.Substring(1)}";
								}
								node.SetAttributeValue(attributeName, fileUri + query);
							}
						}
					}
				}
			}
		}

		private string GetMimeType(File file)
		{
			Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider extensionProvider = new();
			string mimeType = "application/octet-stream";
			extensionProvider.TryGetContentType(file.Path, out mimeType);
			return mimeType;
		}

		private string GetStreamAsString(System.IO.Stream stream)
		{
			using (System.IO.StreamReader reader = new(stream))
			{
				return reader.ReadToEnd();
			}
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel());
		}

		private async Task<ViewModels.Viewer> BuildViewModel()
		{

			return await StaticContentAdminController.BuildSettingsViewModel<ViewModels.Viewer>(null, this.Context.Site, this.Context.Module, this.FileSystemManager);
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel()
		{
			return await StaticContentAdminController.BuildSettingsViewModel<ViewModels.Settings>(null, this.Context.Site, this.Context.Module, this.FileSystemManager);
		}

	}
}