using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish.Controllers
{
	[Extension("Publish")]
	[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
	public class AdminController : Controller
	{
		private Context Context { get; }
		private PageModuleManager PageModuleManager { get; }
		private ArticlesManager ArticlesManager { get; }
		private ListManager ListManager { get; }
		private FileSystemManager FileSystemManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }


		private const string MODULESETTING_CATEGORYLIST_ID = "articles:categorylistid";
		private const string MODULESETTING_LAYOUT = "articles:layout";

		public AdminController(IWebHostEnvironment webHostEnvironment, Context Context, PageModuleManager pageModuleManager, ArticlesManager articlesManager, ListManager listManager, FileSystemManager fileSystemManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.ArticlesManager = articlesManager;
			this.ListManager = listManager;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult Create()
		{
			return View("Editor", BuildEditorViewModel());
		}

		[HttpGet]
		[HttpPost]
		public ActionResult Edit(ViewModels.Editor viewModel, Guid id)
		{
			return View("Editor", BuildEditorViewModel(viewModel, id));
		}

		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_LAYOUT, viewModel.Layout);

			this.PageModuleManager.SaveSettings(this.Context.Module);

			viewModel.Message = "Changes Saved.";
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult SelectAnotherImage(ViewModels.Editor viewModel)
		{
			viewModel.Article.ImageFile.ClearSelection();

			return View("Editor", BuildEditorViewModel(viewModel, viewModel.Article.Id));
		}

		[HttpPost]
		public async Task<ActionResult> UploadImageFile(ViewModels.Editor viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.Article.ImageFile.Parent = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Article.ImageFile.Parent.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					viewModel.Article.ImageFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.Article.ImageFile.Provider, viewModel.Article.ImageFile.Parent.Path, mediaFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult SaveArticle(ViewModels.Editor viewModel)
		{
			//if (viewModel.SelectedImageFile != null)
			//{
			//	try
			//	{
			//		viewModel.Article.ImageFile = this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedImageFile.Provider, viewModel.SelectedImageFile.Path);
			//	}
			//	catch(System.IO.FileNotFoundException)
			//	{
			//		viewModel.Article.ImageFile = null;
			//	}
			//}

			foreach (ViewModels.CategorySelection selection in viewModel.Categories)
			{
				if (selection.IsSelected)
				{
					// add selected category, if it is not already in the categories list
					Models.Category existing = viewModel.Article.Categories.Where(category => category.CategoryItem.Id == selection.Category.CategoryItem.Id).FirstOrDefault();
					if (existing == null)
					{
						viewModel.Article.Categories.Add(selection.Category);
					}
				}
				else
				{
					// remove un-selected category, if it is in the categories list
					Models.Category existing = viewModel.Article.Categories.Where(category => category.CategoryItem.Id == selection.Category.CategoryItem.Id).FirstOrDefault();
					if (existing != null)
					{
						viewModel.Article.Categories.Remove(existing);
					}
				}
			}
			
			this.ArticlesManager.Save(this.Context.Module, viewModel.Article);

			viewModel.Message = "Changes Saved.";
			return View("Settings", BuildSettingsViewModel());
		}

		[HttpPost]
		public ActionResult AddAttachment(ViewModels.Editor viewModel)
		{
			if (viewModel.SelectedAttachmentFile != null)
			{
				viewModel.Article.Attachments.Add(new Models.Attachment() { File = this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedAttachmentFile.Provider, viewModel.SelectedAttachmentFile.Path) });
				viewModel.SelectedAttachmentFile.Path = "";
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> UploadAttachment(ViewModels.Editor viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				Folder folder = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedAttachmentFile.Parent.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					Models.Attachment attachment = new()
					{
						File = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedAttachmentFile.Provider, folder.Path, mediaFile.FileName, fileStream, false)
					};

					viewModel.Article.Attachments.Add(attachment);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult DeleteAttachment(ViewModels.Editor viewModel, Guid id)
		{
			for (int count=0; count < viewModel.Article.Attachments.Count; count++)
			{
				ModelState.Remove($"Article.Attachments[{count}].Id");
			}

			Models.Attachment attachment = viewModel.Article.Attachments.Where(attachment => attachment.Id == id).FirstOrDefault();
			if (attachment != null)
			{
				viewModel.Article.Attachments.Remove(attachment);
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", viewModel);
		}

		private ViewModels.Settings BuildSettingsViewModel()
		{
			return BuildSettingsViewModel(null);
		}

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			{
				if (viewModel == null)
				{
					viewModel = new();
				}

				viewModel.ModuleId = this.Context.Module.Id;
				viewModel.Articles = this.ArticlesManager.List(this.Context.Module);
				viewModel.Lists = this.ListManager.List(this.Context.Site);
				viewModel.CategoryList = this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
				viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");

				viewModel.Layouts = new();
				foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{Constants.EXTENSIONS_ROUTE_PATH}\\Publish\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout=>layout))
				{
					viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
				}

				return viewModel;
			}
		}

		private ViewModels.Editor BuildEditorViewModel()
		{
			return BuildEditorViewModel(null, Guid.Empty);
		}

		private ViewModels.Editor BuildEditorViewModel(ViewModels.Editor input, Guid id)
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

			if (viewModel.Article == null)
			{
				if (id != Guid.Empty)
				{
					viewModel.Article = this.ArticlesManager.Get(this.Context.Site, id);
				}
				else
				{
					viewModel.Article = this.ArticlesManager.CreateNew();
				}
			}

			//if (viewModel.SelectedImageFile == null)
			//{
			//	if (viewModel.Article.ImageFile != null)
			//	{
			//		try
			//		{
			//			viewModel.SelectedImageFile = this.FileSystemManager.GetFile(this.Context.Site, viewModel.Article.ImageFile.Id);
			//		}
			//		catch (System.IO.FileNotFoundException)
			//		{
			//			// if the selected file has been deleted, set the selected file to null and allow the user to select another file
			//			viewModel.SelectedImageFile = null;
			//		}
			//	}
			//	else
			//	{
			//		viewModel.SelectedImageFile = new();
			//	}
			//}

			Guid categoryListId = this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty);
			if (categoryListId != Guid.Empty)
			{
				List categoryList = this.ListManager.Get(categoryListId);

				if (categoryList != null)
				{
					viewModel.Categories.Clear();

					foreach (var item in categoryList.Items)
					{
						ViewModels.CategorySelection selection = new();

						selection.Category = viewModel.Article.Categories.Where(category => category.CategoryItem.Id == item.Id).FirstOrDefault();
						if (selection.Category != null)
						{
							selection.IsSelected = true;
						}
						else
						{
							selection.Category = new Models.Category()
							{
								CategoryItem = item
							};
							selection.IsSelected = false;
						}
						
						viewModel.Categories.Add(selection);
					}
				}
			}

			return viewModel;
		}

	}
}