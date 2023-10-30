using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.XmlDocumentation.Models;
using Nucleus.XmlDocumentation.Models.Serialization;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;

namespace Nucleus.XmlDocumentation.Controllers
{
	[Extension("XmlDocumentation")]
	public class XmlDocumentationController : Controller
	{
		internal const string MODULESETTING_DOCUMENTATION_FOLDER_ID = "xmldocumentation:folderid";
		private const string MODULESETTING_DOCUMENTATION_DEFAULTOPEN = "xmldocumentation:defaultopen";

		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IFileSystemManager FileSystemManager { get; }
		private ICacheManager CacheManager { get; }
		private IContentManager ContentManager { get; }

		public XmlDocumentationController(Context Context, IPageModuleManager pageModuleManager, IContentManager contentManager, IFileSystemManager fileSystemManager, ICacheManager cacheManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.ContentManager = contentManager;
			this.FileSystemManager = fileSystemManager;
			this.CacheManager = cacheManager;
		}

		[HttpGet]
		public async Task<ActionResult> Index(Boolean showApiMenu = true)
		{
			return View("Viewer", await BuildViewModel(showApiMenu));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_DOCUMENTATION_FOLDER_ID, viewModel.DocumentationFolder.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_DOCUMENTATION_DEFAULTOPEN, viewModel.DefaultOpen);

			await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

			await this.ContentManager.Save(this.Context.Module, viewModel.WelcomeMessage);

			this.CacheManager.XmlDocumentationCache().Remove(this.Context.Module.Id);

			return Ok();
		}

		private async Task<ViewModels.Viewer> BuildViewModel(Boolean showMenu)
		{
			Folder documentationFolder;

			ViewModels.Viewer viewModel = new() 
			{
				Page = this.Context.Page,
				ShowMenu = showMenu 
			};

			viewModel.Documents = await this.CacheManager.XmlDocumentationCache().GetAsync(this.Context.Module.Id, async id =>
			{
				List<ApiDocument> results = new();

				try
				{
					documentationFolder = await this.FileSystemManager.ListFolder(this.Context.Site, this.Context.Module.ModuleSettings.Get(MODULESETTING_DOCUMENTATION_FOLDER_ID, Guid.Empty), "(.xml)");
					viewModel.DefaultOpen = this.Context.Module.ModuleSettings.Get(MODULESETTING_DOCUMENTATION_DEFAULTOPEN, true);

					// parse the documentation file, render results
					try
					{

						foreach (File xmlDocument in documentationFolder.Files)
						{
							DocumentationParser parser = new(this.FileSystemManager,this.Context.Site, xmlDocument);
							if (parser.IsValid)
							{
								results.Add(parser.Document);
							}
						}

						DocumentationParser.ParseParams(results);
						DocumentationParser.ParseMixedContent(results);
					}
					catch (System.Exception e)
					{
						viewModel.Message = e.Message;
						results = new();
					}
				}
				catch (System.IO.FileNotFoundException)
				{
					viewModel.Message = "Documentation folder not set.";
					results = new();
				}

				return results;
			});

			if (this.Context.LocalPath.HasValue)
			{
				if (this.Context.LocalPath.Segments.Length > 0 && !this.Context.LocalPath.Segments[0].EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
				{
					// local path contains a single value (a namespace-qualified class name), use it to determine both the document and class name
					viewModel.SelectedDocument = viewModel.Documents.Where(document => this.Context.LocalPath.Segments[0].StartsWith(System.IO.Path.GetFileNameWithoutExtension(document.SourceFileName), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
					if (viewModel.SelectedDocument != null)
					{
						viewModel.SelectedClass = viewModel.SelectedDocument.Classes.Where(cls => cls.ControlId().Equals(this.Context.LocalPath.Segments[0], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();						
					}
				}
				else
				{
					// local path contains the document name and class name separated by "/"
					viewModel.SelectedDocument = viewModel.Documents.Where(document => document.SourceFileName.Equals(this.Context.LocalPath.Segments[0], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
					if (viewModel.SelectedDocument != null && this.Context.LocalPath.Segments.Length > 1)
					{
						viewModel.SelectedClass = viewModel.SelectedDocument.Classes.Where(cls => cls.ControlId().Equals(this.Context.LocalPath.Segments[1], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
					}
				}				
			}

			viewModel.WelcomeMessage = new((await this.ContentManager.List(this.Context.Module)).FirstOrDefault()?.ToHtml());

			return viewModel;
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			if (viewModel.DocumentationFolder == null)
			{
				try
				{
					viewModel.DocumentationFolder = await this.FileSystemManager.GetFolder(this.Context.Site, this.Context.Module.ModuleSettings.Get(MODULESETTING_DOCUMENTATION_FOLDER_ID, Guid.Empty));
					viewModel.DefaultOpen = this.Context.Module.ModuleSettings.Get(MODULESETTING_DOCUMENTATION_DEFAULTOPEN, true);
				}
				catch (System.IO.FileNotFoundException)
				{
					viewModel.DocumentationFolder = new();
				}
			}

			viewModel.WelcomeMessage = (await this.ContentManager.List(this.Context.Module)).FirstOrDefault();


			return viewModel;
		}

	}
}