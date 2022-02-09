using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Modules.Documents.Controllers
{
	[Extension("Documents")]
	public class DocumentsController : Controller
	{
		private const string MODULESETTING_CATEGORYLIST_ID = "documents:categorylistid";
		private const string MODULESETTING_DEFAULTFOLDER_ID = "documents:defaultfolder:provider-id";
		private const string MODULESETTING_ALLOWSORTING = "documents:allowsorting";
		private const string MODULESETTING_LAYOUT = "documents:layout";

		private const string MODULESETTING_SHOW_CATEGORY = "documents:show:category";
		private const string MODULESETTING_SHOW_SIZE = "documents:show:size";
		private const string MODULESETTING_SHOW_MODIFIEDDATE = "documents:show:modifieddate";
		private const string MODULESETTING_SHOW_DESCRIPTION = "documents:show:description";

		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }
		private DocumentsManager DocumentsManager { get; }
		private IListManager ListManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }

		private IPageModuleManager PageModuleManager { get; }

		public DocumentsController(IWebHostEnvironment webHostEnvironment, Context context, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, DocumentsManager documentsManager, IListManager listManager)
		{
			this.Context = context;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
			this.DocumentsManager = documentsManager;
			this.ListManager = listManager;
			this.WebHostEnvironment = webHostEnvironment;
		}

		[HttpGet]
		public async Task<ActionResult> Index(string sortkey, Boolean descending)
		{
			return View("Viewer", await BuildViewModel(sortkey, descending));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Create(ViewModels.Editor viewModel)
		{
			return View("Editor", await BuildEditorViewModel(viewModel, Guid.Empty));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel (viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Edit(ViewModels.Editor viewModel, Guid id)
		{
			return View("Editor", await BuildEditorViewModel(viewModel, id));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SelectAnother(ViewModels.Editor viewModel)
		{
			viewModel.SelectedDocument.File.ClearSelection();
			
			return View("Editor", await BuildEditorViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_DEFAULTFOLDER_ID, viewModel.SelectedFolder?.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_ALLOWSORTING, viewModel.AllowSorting);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_LAYOUT, viewModel.Layout);

			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_CATEGORY, viewModel.ShowCategory);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_DESCRIPTION, viewModel.ShowDescription);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_MODIFIEDDATE, viewModel.ShowModifiedDate);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_SIZE, viewModel.ShowSize);

			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Json(new { Title = "Save Settings", Message = "Settings saved." });
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Editor viewModel)
		{
			await this .DocumentsManager.Save(this.Context.Module, viewModel.SelectedDocument);

			return View("Settings", await BuildSettingsViewModel(new ViewModels.Settings()));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> Delete(ViewModels.Settings viewModel, Guid id)
		{
			Models.Document document = await this.DocumentsManager.Get(this.Context.Site, id);
			await this.DocumentsManager.Delete(document);

			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> MoveUp(ViewModels.Settings viewModel, Guid id)
		{
			await this.DocumentsManager.MoveUp(this.Context.Module, id);
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> MoveDown(ViewModels.Settings viewModel, Guid id)
		{
			await this.DocumentsManager.MoveDown(this.Context.Module, id);
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> UploadFile(ViewModels.Editor viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.SelectedDocument.File.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedDocument.File.Parent.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					viewModel.SelectedDocument.File = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedDocument.File.Provider, viewModel.SelectedDocument.File.Parent.Path, mediaFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", viewModel);
		}


		private async Task<ViewModels.Viewer> BuildViewModel(string sortkey, Boolean descending)
		{
			ViewModels.Viewer viewModel = new();
			viewModel.SortKey = sortkey;
			viewModel.SortDescending = descending;

			viewModel.AllowSorting = this.Context.Module.ModuleSettings.Get(MODULESETTING_ALLOWSORTING, false);
			viewModel.ShowCategory = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORY, true);
			viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_DESCRIPTION, true);
			viewModel.ShowModifiedDate= this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_MODIFIEDDATE, true);
			viewModel.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);

			System.IO.DirectoryInfo thisFolder = new(this.GetType().Assembly.Location);
					

			viewModel.Layout = $"ViewerLayouts/{this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table")}.cshtml";

			foreach (Models.Document document in await this.DocumentsManager.List(this.Context.Site, this.Context.Module))
			{
				ViewModels.Viewer.DocumentInfo docInfo = new()
				{
					Id = document.Id,
					Title = document.Title,
					Description=document.Description,
					Category = document.Category,
					File = document.File,
					SortOrder=document.SortOrder					
				};

				if (document.File != null)
				{
					File file = await this.FileSystemManager.GetFile(this.Context.Site, document.File.Id);
					file.Parent.Permissions = await this.FileSystemManager.ListPermissions(file.Parent);

					if (User.HasViewPermission(this.Context.Site, file.Parent))
					{
						if (file != null)
						{
							docInfo.ModifiedDate = file.DateModified;
							docInfo.Size = file.Size;
						}

						viewModel.Documents.Add(docInfo);
					}
				}
			}

			if (!string.IsNullOrEmpty(sortkey))
			{
				System.Reflection.PropertyInfo prop = typeof(ViewModels.Viewer.DocumentInfo).GetProperties().Where(prop => prop.Name == sortkey).FirstOrDefault();
				if (prop != null)
				{					
					viewModel.Documents = viewModel.Documents.OrderBy(document => prop.GetValue(document)).ThenBy(document => document.Title).ToList();
					
					if (!descending)
					{
						viewModel.Documents.Reverse();
					}
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

			viewModel.Documents = await this.DocumentsManager.List(this.Context.Site, this.Context.Module);
			viewModel.Lists = await this.ListManager.List(this.Context.Site);
			viewModel.CategoryList = await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
			viewModel.AllowSorting = this.Context.Module.ModuleSettings.Get(MODULESETTING_ALLOWSORTING, false);
			viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");

			viewModel.ShowCategory = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORY, true);
			viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_DESCRIPTION, true);
			viewModel.ShowModifiedDate = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_MODIFIEDDATE, true);
			viewModel.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);

			if (viewModel.SelectedFolder == null)
			{
				try
				{
					viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, this.Context.Module.ModuleSettings.Get(MODULESETTING_DEFAULTFOLDER_ID, Guid.Empty));
				}
				catch (System.IO.FileNotFoundException)
				{
					viewModel.SelectedFolder = null;
				}
			}

			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{RoutingConstants.EXTENSIONS_ROUTE_PATH}\\Documents\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}
			return viewModel;
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor input)
		{
			ViewModels.Editor viewModel;

			if (input == null)
			{
				viewModel = new ViewModels.Editor();
			}
			else
			{
				viewModel = input;
			}

			viewModel.SelectedDocument = input.SelectedDocument;

			viewModel.Categories = (await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty)))?.Items;

			return viewModel;
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor input, Guid documentId)
		{
			if (input.SelectedDocument == null)
			{
				if (documentId == Guid.Empty)
				{
					input.SelectedDocument = await this.DocumentsManager.CreateNew();
				}
				else
				{
					input.SelectedDocument = await this.DocumentsManager.Get(this.Context.Site, documentId);
				}
			}

			return await BuildEditorViewModel(input);
		}
	}
}