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
using Nucleus.Modules.Publish.Models;

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
    private string ViewerLayoutFolder { get; }
		    
    public AdminController(IWebHostEnvironment webHostEnvironment, Context Context, IPageModuleManager pageModuleManager, ArticlesManager articlesManager, IListManager listManager, IFileSystemManager fileSystemManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.ArticlesManager = articlesManager;
			this.ListManager = listManager;
			this.FileSystemManager = fileSystemManager;
    
			this.ViewerLayoutFolder = $"{webHostEnvironment.ContentRootPath}\\{Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER}\\Publish\\Views\\ViewerLayouts\\";
    }

    public async Task<ActionResult> Settings()
    {
      return View("Settings", await BuildSettingsViewModel(null));
    }

    [HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> Create(ViewModels.Editor viewModel)
		{
			return View("Editor", await BuildEditorViewModel());
		}

		/// <summary>
		/// Initial load for an existing record.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		/// <remarks>
		/// Sets the viewmodel "UseLayout" to _PopupEditor when mode=standalone in order to render a full page.
		/// </remarks>
		[HttpGet]
		public async Task<ActionResult> Edit(Guid id, string mode)
		{
			return View("Editor", await BuildEditorViewModel(id, mode?.Equals("standalone", StringComparison.OrdinalIgnoreCase) == true));
		}

		/// <summary>
		/// Handles "Postback" of the editor when a user adds an attachment, etc.
		/// </summary>
		/// <param name="viewModel"></param>
		/// <returns></returns>
		/// <remarks>
		/// This action does NOT set the viewmodel "UseLayout" to _PopupEditor when mode=standalone because we only want a partial page for "postbacks".
		/// </remarks>
		[HttpPost]
		public async Task<ActionResult> Editor(ViewModels.Editor viewModel)
		{
			return View("Editor", await BuildEditorViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			viewModel.SetSettings(this.Context.Module);
			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Json(new { Title = "Save Settings", Message = "Settings saved." });
		}

		[HttpPost]
		public async Task<ActionResult> SelectAnotherImage(ViewModels.Editor viewModel)
		{
			viewModel.Article.ImageFile.ClearSelection();

			return View("Editor", await BuildEditorViewModel(viewModel));
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
			foreach (ViewModels.ArticleCategorySelection selection in viewModel.Categories)
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

			if (viewModel.Article.PublishDate.HasValue)
			{
				viewModel.Article.PublishDate = TimeZoneInfo.ConvertTimeToUtc(viewModel.Article.PublishDate.Value, this.ControllerContext.HttpContext.Request.GetUserTimeZone());
			}

			if (viewModel.Article.ExpireDate.HasValue)
			{
				viewModel.Article.ExpireDate = TimeZoneInfo.ConvertTimeToUtc(viewModel.Article.ExpireDate.Value, this.ControllerContext.HttpContext.Request.GetUserTimeZone());
			}

			await this.ArticlesManager.Save(this.Context.Module, viewModel.Article);

			if (viewModel.Standalone)
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
			if (viewModel.SelectedAttachmentFile != null && viewModel.SelectedAttachmentFile.Id != Guid.Empty)
			{
				viewModel.Article.Attachments.Add(new Models.Attachment() { File = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedAttachmentFile.Id) });
				viewModel.SelectedAttachmentFile.ClearSelection();
			}
			else
			{
				return BadRequest("Please select a file to be attached to the article.");
			}

			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> SelectAnotherAttachment(ViewModels.Editor viewModel)
		{
			viewModel.SelectedAttachmentFile.ClearSelection();

			return View("Editor", await BuildEditorViewModel(viewModel));
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
          viewModel.GetSettings(this.Context.Module);
        }

        viewModel.Articles = await this.ArticlesManager.List(this.Context.Module);
				viewModel.Lists = await this.ListManager.List(this.Context.Site);
				viewModel.CategoryList = await this.ListManager.Get(viewModel.CategoryListId);

        ListLayouts(viewModel);

        return viewModel;
			}
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel()
		{
			ViewModels.Editor viewModel = new();
			viewModel.Article = await this.ArticlesManager.CreateNew();
			
			await GetCategories(viewModel);
			return viewModel;
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel(Guid id, Boolean standalone)
		{
			ViewModels.Editor viewModel = new ();
			if (standalone)
			{
				viewModel.Standalone = true;
				viewModel.UseLayout = "_PopupEditor";
			}

			if (id != Guid.Empty)
			{
				viewModel.Article = await this.ArticlesManager.Get(this.Context.Site, id);

				if (viewModel.Article.PublishDate.HasValue)
				{
					viewModel.Article.PublishDate = TimeZoneInfo.ConvertTimeFromUtc(viewModel.Article.PublishDate.Value, this.ControllerContext.HttpContext.Request.GetUserTimeZone());				
				}
				if (viewModel.Article.ExpireDate.HasValue)
				{
					viewModel.Article.ExpireDate = TimeZoneInfo.ConvertTimeFromUtc(viewModel.Article.ExpireDate.Value, this.ControllerContext.HttpContext.Request.GetUserTimeZone());
				}
			}
			else
			{
				viewModel.Article = await this.ArticlesManager.CreateNew();
			}

			await GetCategories(viewModel);

			return viewModel;
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor viewModel)
		{
			viewModel.SelectedAttachmentFile = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.SelectedAttachmentFile);
			await GetCategories(viewModel);
			return viewModel;
		}

		private async Task GetCategories(ViewModels.Editor viewModel)
		{
			Settings settings = new ViewModels.Settings();
			settings.GetSettings(this.Context.Module);

			if (settings.CategoryListId != Guid.Empty)
			{
				List categoryList = await this.ListManager.Get(settings.CategoryListId);

				if (categoryList != null)
				{
					viewModel.Categories.Clear();

					foreach (var item in categoryList.Items)
					{
						ViewModels.ArticleCategorySelection selection = new();

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
		}
    
		private void ListLayouts(ViewModels.Settings viewModel)
    {
      viewModel.LayoutOptions.ViewerLayouts = viewModel.LayoutOptions.ListViewerLayouts(this.ViewerLayoutFolder);
			
			if (String.IsNullOrEmpty(viewModel.LayoutOptions.ViewerLayout))
			{
        viewModel.LayoutOptions.ViewerLayout = viewModel.LayoutOptions.ViewerLayouts.FirstOrDefault();
			}

			viewModel.LayoutOptions.MasterLayouts = viewModel.LayoutOptions.ListMasterLayouts(this.ViewerLayoutFolder);
      viewModel.LayoutOptions.ArticleLayouts = viewModel.LayoutOptions.ListArticleLayouts(this.ViewerLayoutFolder);
    }
  }
}