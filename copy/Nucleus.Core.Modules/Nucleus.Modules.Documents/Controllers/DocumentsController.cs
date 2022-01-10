using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

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
		private FileSystemManager FileSystemManager { get; }
		private DocumentsManager DocumentsManager { get; }
		private ListManager ListManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }

		private PageModuleManager PageModuleManager { get; }

		public DocumentsController(IWebHostEnvironment webHostEnvironment, Context Context, PageModuleManager pageModuleManager, FileSystemManager fileSystemManager, DocumentsManager documentsManager, ListManager listManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
			this.DocumentsManager = documentsManager;
			this.ListManager = listManager;
			this.WebHostEnvironment = webHostEnvironment;
		}

		[HttpGet]
		public ActionResult Index(string sortkey, Boolean descending)
		{
			return View("Viewer", BuildViewModel(sortkey, descending));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Create(ViewModels.Editor viewModel)
		{
			return View("Editor", BuildEditorViewModel(viewModel, Guid.Empty));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Edit(ViewModels.Editor viewModel, Guid id)
		{
			return View("Editor", BuildEditorViewModel(viewModel, id));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SelectAnother(ViewModels.Editor viewModel)
		{
			viewModel.SelectedDocument.File.ClearSelection();
			
			return View("Editor", BuildEditorViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			//this.Context.Module.ModuleSettings.Set(MODULESETTING_DEFAULTFOLDER_ID, this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Provider, viewModel.SelectedFolder.Path)?.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_DEFAULTFOLDER_ID, viewModel.SelectedFolder?.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_ALLOWSORTING, viewModel.AllowSorting);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_LAYOUT, viewModel.Layout);

			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_CATEGORY, viewModel.ShowCategory);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_DESCRIPTION, viewModel.ShowDescription);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_MODIFIEDDATE, viewModel.ShowModifiedDate);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_SIZE, viewModel.ShowSize);

			this.PageModuleManager.SaveSettings(this.Context.Module);

			viewModel.Message = "Changes Saved.";
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult Save(ViewModels.Editor viewModel)
		{
			//viewModel.SelectedDocument.Provider = viewModel.SelectedFile.Provider;
			//viewModel.SelectedDocument.Path = viewModel.SelectedFile.Path;
			//viewModel.SelectedDocument.File =this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedFile.Provider, viewModel.SelectedFile.Path);
			//viewModel.SelectedDocument.File = viewModel.SelectedFile;
			this.DocumentsManager.Save(this.Context.Module, viewModel.SelectedDocument);

			return View("Settings", BuildSettingsViewModel(new ViewModels.Settings()));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult Delete(ViewModels.Settings viewModel, Guid id)
		{
			Models.Document document = this.DocumentsManager.Get(this.Context.Site, id);
			this.DocumentsManager.Delete(document);

			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult MoveUp(ViewModels.Settings viewModel, Guid id)
		{
			this.DocumentsManager.MoveUp(this.Context.Module, id);
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult MoveDown(ViewModels.Settings viewModel, Guid id)
		{
			this.DocumentsManager.MoveDown(this.Context.Module, id);
			return View("Settings", BuildSettingsViewModel(viewModel));
		}


		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> UploadFile(ViewModels.Editor viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.SelectedDocument.File.Parent = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedDocument.File.Parent.Id);
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


		private ViewModels.Viewer BuildViewModel(string sortkey, Boolean descending)
		{
			ViewModels.Viewer viewModel = new();
			viewModel.ControlId = $"_{Guid.NewGuid().ToString("N")}";
			viewModel.SortKey = sortkey;
			viewModel.SortDescending = descending;

			viewModel.AllowSorting = this.Context.Module.ModuleSettings.Get(MODULESETTING_ALLOWSORTING, false);
			viewModel.ShowCategory = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORY, true);
			viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_DESCRIPTION, true);
			viewModel.ShowModifiedDate= this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_MODIFIEDDATE, true);
			viewModel.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);

			System.IO.DirectoryInfo thisFolder = new System.IO.DirectoryInfo(this.GetType().Assembly.Location);
					

			viewModel.Layout = $"ViewerLayouts/{this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table")}.cshtml";

			foreach (Models.Document document in this.DocumentsManager.List(this.Context.Site, this.Context.Module))
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
					File file = this.FileSystemManager.GetFile(this.Context.Site, document.File.Provider, document.File.Path);

					if (file != null)
					{
						docInfo.ModifiedDate = file.DateModified;
						docInfo.Size = file.Size;
					}

					viewModel.Documents.Add(docInfo);
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

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ModuleId = this.Context.Module.Id;
			viewModel.Documents = this.DocumentsManager.List(this.Context.Site, this.Context.Module);
			viewModel.Lists = this.ListManager.List(this.Context.Site);
			viewModel.CategoryList = this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
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
					viewModel.SelectedFolder = this.FileSystemManager.GetFolder(this.Context.Site, this.Context.Module.ModuleSettings.Get<Guid>(MODULESETTING_DEFAULTFOLDER_ID, Guid.Empty));
				}
				catch (System.IO.FileNotFoundException)
				{
					viewModel.SelectedFolder = null;
				}
			}

			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{Constants.EXTENSIONS_ROUTE_PATH}\\Documents\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}
			return viewModel;
		}

		private ViewModels.Editor BuildEditorViewModel(ViewModels.Editor input)
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

			viewModel.ModuleId = this.Context.Module.Id;
			viewModel.SelectedDocument = input.SelectedDocument;

			viewModel.Categories = this.ListManager.Get(this.Context.Module.ModuleSettings.Get<Guid>(MODULESETTING_CATEGORYLIST_ID, Guid.Empty)).Items;

			//if (viewModel.SelectedFile == null)
			//{
			//	viewModel.SelectedFile = new();
			//}

			//if (viewModel.SelectedDocument.File != null)
			//{
			//	try
			//	{
			//		//viewModel.SelectedFile = this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedDocument.File.Id);
			//		viewModel.SelectedFile = viewModel.SelectedDocument.File;
			//	}
			//	catch (System.IO.FileNotFoundException)
			//	{
			//		// if the selected file has been deleted, set the selected file to null and allow the user to select another file
			//		viewModel.SelectedFile = null;
			//	}
			//}

			return viewModel;
		}

		private ViewModels.Editor BuildEditorViewModel(ViewModels.Editor input, Guid documentId)
		{
			if (input.SelectedDocument == null)
			{
				if (documentId == Guid.Empty)
				{
					input.SelectedDocument = this.DocumentsManager.CreateNew();

					//input.SelectedFile = new();

					//try
					//{
					//	Folder folder = this.FileSystemManager.GetFolder(this.Context.Site, this.Context.Module.ModuleSettings.Get<Guid>(MODULESETTING_DEFAULTFOLDER_ID, Guid.Empty));
					//	input.SelectedFile.Provider = folder.Provider;
					//	input.SelectedFile.Parent = folder;
					//}
					//catch (System.IO.FileNotFoundException)
					//{
					//	input.SelectedFile = null;
					//}
				}
				else
				{
					input.SelectedDocument = this.DocumentsManager.Get(this.Context.Site, documentId);
				}
			}
			//else
			//{
			//	try
			//	{
			//		// The user has selected a different folder
			//		//input.SelectedFile = this.FileSystemManager.GetFile(this.Context.Site, input.SelectedFile.Provider, input.SelectedFile.Path);
			//	}
			//	catch (System.IO.FileNotFoundException)
			//	{
			//		input.SelectedFile = null;
			//	}
			//}
			return BuildEditorViewModel(input);
		}
	}
}