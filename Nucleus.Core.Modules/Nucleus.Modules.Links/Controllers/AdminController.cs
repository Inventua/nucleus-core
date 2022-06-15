using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Managers;
using System;
using System.Linq;
using Nucleus.Modules.Links.Models;
using System.Threading.Tasks;
using Nucleus.Extensions;

namespace Nucleus.Modules.Links.Controllers
{
	[Extension("Links")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
	public class AdminController : Controller
	{
		public const string MODULESETTING_CATEGORYLIST_ID = "links:categorylistid";
		public const string MODULESETTING_LAYOUT = "links:layout";
		public const string MODULESETTING_OPEN_NEW_WINDOW = "links:opennewwindow";

		private Context Context { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }
		private LinksManager LinksManager { get; }
		private IFileSystemManager FileSystemManager { get; }
		private IListManager ListManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }

		public AdminController(IWebHostEnvironment webHostEnvironment, Context Context, IPageManager pageManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, LinksManager linksManager, IListManager listManager)
		{
			this.WebHostEnvironment = webHostEnvironment; 
			this.Context = Context;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
			this.LinksManager = linksManager;
			this.ListManager = listManager;			
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
			return View("Editor", await BuildEditorViewModel(null, Guid.Empty, false));
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Editor(ViewModels.Editor viewModel, Guid id, string mode)
		{
			return View("Editor", await BuildEditorViewModel(viewModel, id, mode == "Standalone"));
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> DeleteLink(ViewModels.Settings viewModel, Guid id)
		{
			Link link = await this.LinksManager.Get(this.Context.Site, id);
			await this.LinksManager.Delete(link);
			return View("_LinksList", await BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> SelectAnother(ViewModels.Editor viewModel)
		{
			viewModel.Link.LinkFile.File.ClearSelection();

			return View("Editor", await BuildEditorViewModel(viewModel, Guid.Empty, viewModel.UseLayout=="_PopupEditor"));
		}

		[HttpPost]
		public async Task<ActionResult> MoveUp(ViewModels.Settings viewModel, Guid id)
		{
			await this.LinksManager.MoveUp(this.Context.Module, id);
			return View("_LinksList", await BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> MoveDown(ViewModels.Settings viewModel, Guid id)
		{
			await this.LinksManager.MoveDown(this.Context.Module, id);
			return View("_LinksList", await BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> UploadFile(ViewModels.Editor viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					viewModel.Link.LinkFile.File = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.Link.LinkFile.File.Provider, viewModel.Link.LinkFile.File.Parent.Path, mediaFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", viewModel);
		}

		[HttpGet]
		public async Task<ActionResult> GetChildPages(Guid id)
		{
			ViewModels.PageIndexPartial viewModel = new();

			viewModel.FromPage = await this.PageManager.Get(id);

			viewModel.Pages = await this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					await this.PageManager.Get(id),
					ControllerContext.HttpContext.User,
					1,
					true,
					false,
					true
				);

			return View("_PageMenu", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> SaveLink(ViewModels.Editor viewModel)
		{
			// Category is not mandatory
			ModelState.Remove("Link.Category.Id"); 
			ModelState.Remove("Link.Category.Name");
			
			ModelState.Remove("Link.LinkPage.Page.Name");

			if (ModelState.IsValid)
			{
				await this.LinksManager.Save(this.Context.Module, viewModel.Link);
			}
			else
			{
				return BadRequest(ModelState);
			}

			if (viewModel.UseLayout == "_PopupEditor")
			{
				return Ok();
			}
			else
			{
				return View("_LinksList", await BuildSettingsViewModel(null));
			}
		}

		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_LAYOUT, viewModel.Layout);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_OPEN_NEW_WINDOW, viewModel.NewWindow);

			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Json(new { Title = "Changes Saved", Message = "Your changes have been saved." });
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.CategoryList = await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
			viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");
			viewModel.NewWindow = this.Context.Module.ModuleSettings.Get(MODULESETTING_OPEN_NEW_WINDOW, false); 

			viewModel.Lists = await this.ListManager.List(this.Context.Site);

			viewModel.Links = await this.LinksManager.List(this.Context.Site, this.Context.Module);


			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{FolderOptions.EXTENSIONS_FOLDER}\\Links\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}
			
			return viewModel;
		}

		private async Task<ViewModels.Editor> BuildEditorViewModel(ViewModels.Editor input, Guid id, Boolean standalone)
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

			if (standalone)
			{
				viewModel.UseLayout = "_PopupEditor";
			}

			if (viewModel.Link == null)
			{
				if (id != Guid.Empty)
				{
					viewModel.Link = await this.LinksManager.Get(this.Context.Site, id);
				}
				else
				{
					viewModel.Link = await this.LinksManager.CreateNew();
				}
			}

			viewModel.CategoryList = await this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));

			switch (viewModel.Link.LinkType)
			{
				//case Models.LinkTypes.Url:
				//case Models.LinkTypes.File:
				case Models.LinkTypes.Page:
					viewModel.PageMenu = (await this.PageManager.GetAdminMenu(this.Context.Site, null, ControllerContext.HttpContext.User, 1, true, false, true));
					break;
			}

			viewModel.LinkTypes = new();
			viewModel.LinkTypes.Add(Models.LinkTypes.Url, "Url");
			viewModel.LinkTypes.Add(Models.LinkTypes.Page, "Page");
			viewModel.LinkTypes.Add(Models.LinkTypes.File, "File");


			return viewModel;
		}

	}
}