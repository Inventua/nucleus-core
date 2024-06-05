using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

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
		private const string MODULESETTING_DEFAULTSORTORDER = "documents:defaultsortorder";

		private IWebHostEnvironment WebHostEnvironment { get; }
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }
		private DocumentsManager DocumentsManager { get; }
		private IListManager ListManager { get; }
		private FolderOptions FolderOptions { get; }

		private IPageModuleManager PageModuleManager { get; }

		public DocumentsController(IWebHostEnvironment webHostEnvironment, IOptions<FolderOptions> folderOptions, Context context, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, DocumentsManager documentsManager, IListManager listManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = context;
			this.FolderOptions = folderOptions.Value;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
			this.DocumentsManager = documentsManager;
			this.ListManager = listManager;
		}

		[HttpGet]
		public async Task<ActionResult> Index(string sortkey, Boolean? descending)
		{
			return View("Viewer", await BuildViewModel(sortkey, descending != false));
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
		public async Task<ActionResult> Edit(ViewModels.Editor viewModel, Guid id, string mode)
		{
			if (mode?.Equals("standalone", StringComparison.OrdinalIgnoreCase) == true)
			{
				viewModel.UseLayout = "_PopupEditor";
			}
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
			this.Context.Module.ModuleSettings.Set(MODULESETTING_DEFAULTSORTORDER, viewModel.DefaultSortOrder);

			await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

			return Json(new { Title = "Save Settings", Message = "Settings saved.", Icon = "alert" });
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Editor viewModel)
		{
			await this .DocumentsManager.Save(this.Context.Module, viewModel.SelectedDocument);

			if (viewModel.UseLayout == "_PopupEditor")
			{
				return Ok();
			}
			else
			{
				return View("Settings", await BuildSettingsViewModel(new ViewModels.Settings()));
			}
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> UpdateTitle(Guid id, string value)
		{
			Models.Document document = await this.DocumentsManager.Get(this.Context.Site, id);

			if (document == null)
			{
				return BadRequest();
			}
			else
			{
				document.Title = value;
			}

			await this.DocumentsManager.Save(this.Context.Module, document);

			return Ok();
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> UpdateDescription(Guid id, string value)
		{
			Models.Document document = await this.DocumentsManager.Get(this.Context.Site, id);

			if (document == null)
			{
				return BadRequest();
			}
			else
			{
				document.Description = value;
			}

			await this.DocumentsManager.Save(this.Context.Module, document);

			return Ok();
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

			return View("Editor", await BuildEditorViewModel(viewModel));
		}


		private async Task<ViewModels.Viewer> BuildViewModel(string sortkey, Boolean descending)
		{
			ViewModels.Viewer viewModel = new();
			viewModel.SortKey = sortkey;
			viewModel.SortDescending = descending;

			viewModel.Page = this.Context.Page;
			viewModel.AllowSorting = this.Context.Module.ModuleSettings.Get(MODULESETTING_ALLOWSORTING, false);
			viewModel.ShowCategory = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORY, true);
			viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_DESCRIPTION, true);
			viewModel.ShowModifiedDate= this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_MODIFIEDDATE, true);
			viewModel.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);
				
			if (String.IsNullOrEmpty(viewModel.SortKey))
			{
				viewModel.SortKey = this.Context.Module.ModuleSettings.Get(MODULESETTING_DEFAULTSORTORDER, "");
			}

			string layoutPath = $"ViewerLayouts/{this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table")}.cshtml";

			//if (!System.IO.File.Exists($"{this.FolderOptions.GetExtensionFolder("Documents", false)}/Views/{layoutPath}"))
			//{
			//	layoutPath = $"ViewerLayouts/Table.cshtml";
			//}

      // Check that the selected Viewer Layout file exists, if not, use the default (Table.cshtml)
      IFileInfo layoutFile = this.WebHostEnvironment.ContentRootFileProvider.GetFileInfo($"Extensions/Documents/Views/{layoutPath}");
      if (!layoutFile.Exists)
      {        
        layoutPath = $"ViewerLayouts/Table.cshtml";
      }

      viewModel.Layout = layoutPath;

			foreach (Models.Document document in await this.DocumentsManager.List(this.Context.Site, this.Context.Module))
			{
				ViewModels.Viewer.DocumentInfo docInfo = new()
				{
					Id = document.Id,
					Title = document.Title,
					Description=document.Description,
					Category = document.Category,
					File = document.File,
					SortOrder = document.SortOrder					
				};

				if (document.File != null)
				{
					File file = await this.FileSystemManager.GetFile(this.Context.Site, document.File.Id);

					// As of 1.0.1, .GetFile(site, id) always populates file.parent & file.parent.permissions
					// .GetFolder always reads permissions, and mostly comes from cache, so it will yield better performance
					// than calling .ListPermissions.
					//file.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, file.Parent.Id);
					//file.Parent.Permissions = await this.FileSystemManager.ListPermissions(file.Parent);

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

			if (!string.IsNullOrEmpty(viewModel.SortKey))
			{
				System.Reflection.PropertyInfo prop = typeof(ViewModels.Viewer.DocumentInfo).GetProperties().Where(prop => prop.Name == viewModel.SortKey).FirstOrDefault();
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

			viewModel.SortOrders = new List<SelectListItem>() 
			{  
				new("Sort Index", ""),
				new("Title", "Title"),
				new("Category", "Category"),
				new("Modified Date", "ModifiedDate"),
				new("Size", "Size"),
				new("Description", "Description")
			};

			viewModel.Documents = await this.DocumentsManager.List(this.Context.Site, this.Context.Module);
			viewModel.Lists = await this.ListManager.List(this.Context.Site);
			viewModel.CategoryList = await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
			viewModel.AllowSorting = this.Context.Module.ModuleSettings.Get(MODULESETTING_ALLOWSORTING, false);
			viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");

			viewModel.ShowCategory = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORY, true);
			viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_DESCRIPTION, true);
			viewModel.ShowModifiedDate = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_MODIFIEDDATE, true);
			viewModel.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);
			viewModel.DefaultSortOrder = this.Context.Module.ModuleSettings.Get(MODULESETTING_DEFAULTSORTORDER, "");

			if (viewModel.SelectedFolder == null)
			{
				try
				{
					viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, this.Context.Module.ModuleSettings.Get(MODULESETTING_DEFAULTFOLDER_ID, Guid.Empty));
				}
				catch (Exception)
				{
					viewModel.SelectedFolder = null;
				}
			}

			viewModel.Layouts = new();

			// foreach (string file in System.IO.Directory.EnumerateFiles($"{this.FolderOptions.GetExtensionFolder("Documents", false)}/Views/ViewerLayouts/", "*.cshtml").OrderBy(layout => layout))
			// {
			//	 viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			// }

			foreach (IFileInfo file in this.WebHostEnvironment.ContentRootFileProvider.GetDirectoryContents("Extensions/Documents/Views/ViewerLayouts")
        .Where(fileInfo => System.IO.Path.GetExtension(fileInfo.Name).Equals(".cshtml", StringComparison.OrdinalIgnoreCase))
        .OrderBy(fileInfo => fileInfo.Name))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file.Name));
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
			viewModel.SelectedDocument.File = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.SelectedDocument.File);
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