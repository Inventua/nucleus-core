using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Extensions;

namespace Nucleus.Modules.Publish.Controllers
{
	[Extension("Publish")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
	public class AdminController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private ArticlesManager ArticlesManager { get; }
		private IListManager ListManager { get; }
		private IFileSystemManager FileSystemManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }

		private const string MODULESETTING_CATEGORYLIST_ID = "articles:categorylistid";
		private const string MODULESETTING_LAYOUT = "articles:layout";

		public AdminController(IWebHostEnvironment webHostEnvironment, Context Context, IPageModuleManager pageModuleManager, ArticlesManager articlesManager, IListManager listManager, IFileSystemManager fileSystemManager)
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
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> Create()
		{
			return View("Editor", await BuildEditorViewModel());
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Edit(ViewModels.Editor viewModel, Guid id, string mode)
		{
			if (mode == "Standalone")
			{
				//if (mode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)				
				viewModel.UseLayout = "_PopupEditor";				
			}
			return View("Editor", await BuildEditorViewModel(viewModel, id));
		}

		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_LAYOUT, viewModel.Layout);

			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Json(new { Title = "Save Settings", Message = "Settings saved." });
		}

		[HttpPost]
		public async Task<ActionResult> SelectAnotherImage(ViewModels.Editor viewModel)
		{
			viewModel.Article.ImageFile.ClearSelection();

			return View("Editor", await BuildEditorViewModel(viewModel, viewModel.Article.Id));
		}

		[HttpPost]
		public async Task<ActionResult> UploadImageFile(ViewModels.Editor viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.Article.ImageFile.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Article.ImageFile.Parent.Id);
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
		public async Task<ActionResult> SaveArticle(ViewModels.Editor viewModel)
		{
			foreach (ViewModels.CategorySelection selection in viewModel.Categories)
			{
				if (selection.IsSelected)
				{
					// add selected category, if it is not already in the categories list
					Models.Category existing = viewModel.Article.Categories.Where(category => category.CategoryListItem.Id == selection.Category.CategoryListItem.Id).FirstOrDefault();
					if (existing == null)
					{
						viewModel.Article.Categories.Add(selection.Category);
					}
				}
				else
				{
					// remove un-selected category, if it is in the categories list
					Models.Category existing = viewModel.Article.Categories.Where(category => category.CategoryListItem.Id == selection.Category.CategoryListItem.Id).FirstOrDefault();
					if (existing != null)
					{
						viewModel.Article.Categories.Remove(existing);
					}
				}
			}

			await this.ArticlesManager.Save(this.Context.Module, viewModel.Article);

			if (viewModel.UseLayout == "_PopupEditor")
			{
				return Ok();
			}
			else
			{
				return View("Settings", await BuildSettingsViewModel());
			}
		}

		[HttpPost]
		public async Task<ActionResult> DeleteArticle(ViewModels.Editor viewModel, Guid id)
		{
			viewModel.Article = await this.ArticlesManager.Get(this.Context.Site, id);
			await this.ArticlesManager.Delete(viewModel.Article);

			return View("Settings", await BuildSettingsViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> AddAttachment(ViewModels.Editor viewModel)
		{
			if (viewModel.SelectedAttachmentFile != null)
			{
				viewModel.Article.Attachments.Add(new Models.Attachment() { File = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedAttachmentFile.Id) });
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
				Folder folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedAttachmentFile.Parent.Id);
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

		private async Task<ViewModels.Settings> BuildSettingsViewModel()
		{
			return await BuildSettingsViewModel(null);
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			{
				if (viewModel == null)
				{
					viewModel = new();
				}

				viewModel.Articles = await this.ArticlesManager.List(this.Context.Module);
				viewModel.Lists = await this.ListManager.List(this.Context.Site);
				viewModel.CategoryList = await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
				viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");

				viewModel.Layouts = new();
				foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{RoutingConstants.EXTENSIONS_ROUTE_PATH}\\Publish\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout=>layout))
				{
					viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
				}

				return viewModel;
			}
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel()
		{
			return await BuildEditorViewModel(null, Guid.Empty);
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor input, Guid id)
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
					viewModel.Article = await this.ArticlesManager.Get(this.Context.Site, id);
				}
				else
				{
					viewModel.Article = await this.ArticlesManager.CreateNew();
				}
			}

			Guid categoryListId = this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty);
			if (categoryListId != Guid.Empty)
			{
				List categoryList = await this.ListManager.Get(categoryListId);

				if (categoryList != null)
				{
					viewModel.Categories.Clear();

					foreach (var item in categoryList.Items)
					{
						ViewModels.CategorySelection selection = new();

						selection.Category = viewModel.Article.Categories.Where(category => category.CategoryListItem.Id == item.Id).FirstOrDefault();
						if (selection.Category != null)
						{
							selection.IsSelected = true;
						}
						else
						{
							selection.Category = new()
							{
								CategoryListItem = item
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