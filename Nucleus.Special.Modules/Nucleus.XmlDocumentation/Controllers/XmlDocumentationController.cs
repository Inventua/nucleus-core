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
		private const string MODULESETTING_DOCUMENTATION_FOLDER_ID = "xmldocumentation:folderid";
		private const string MODULESETTING_DOCUMENTATION_DEFAULTOPEN = "xmldocumentation:defaultopen";

		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IFileSystemManager FileSystemManager { get; }

		public XmlDocumentationController(Context Context, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Viewer", await BuildViewModel ());
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
			
			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		private async Task<ViewModels.Viewer> BuildViewModel()
		{
			ViewModels.Viewer viewModel = new();
			Folder documentationFolder;

			viewModel.Page = this.Context.Page;

			try
			{
				documentationFolder = await this.FileSystemManager.ListFolder(this.Context.Site, this.Context.Module.ModuleSettings.Get(MODULESETTING_DOCUMENTATION_FOLDER_ID, Guid.Empty), "(.xml)");
				viewModel.DefaultOpen = this.Context.Module.ModuleSettings.Get(MODULESETTING_DOCUMENTATION_DEFAULTOPEN, true);

				// parse the documentation file, render results
				try
				{
					viewModel.Documents = new();
				
					foreach (File xmlDocument in documentationFolder.Files)
					{
						DocumentationParser parser = new(this.FileSystemManager.GetFileContents(this.Context.Site, xmlDocument), xmlDocument.Name);
						if (parser.IsValid)
						{
							viewModel.Documents.Add(parser.Document);
						}
					}

					DocumentationParser.ParseMixedContent(viewModel.Documents);
				}
				catch (System.Exception e)
				{
					viewModel.Message = e.Message;
					viewModel.Documents = new();
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				viewModel.Message = "Documentation folder not set.";
				viewModel.Documents = new();
			}

			if (this.Context.LocalPath.HasValue)
			{
				if (this.Context.LocalPath.Segments.Length > 0)
				{
					viewModel.SelectedDocument = viewModel.Documents.Where(document => document.SourceFileName.Equals(this.Context.LocalPath.Segments[0], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
				}
				if (viewModel.SelectedDocument != null && this.Context.LocalPath.Segments.Length > 1)
				{
					viewModel.SelectedClass = viewModel.SelectedDocument.Classes.Where(cls => cls.FullName.Equals(this.Context.LocalPath.Segments[1], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
				}
			}

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

			return viewModel;
		}

	}
}