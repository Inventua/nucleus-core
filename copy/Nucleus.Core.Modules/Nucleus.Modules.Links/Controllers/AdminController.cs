using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using System;
using System.Linq;
using Nucleus.Modules.Links.Models;
using System.Threading.Tasks;

namespace Nucleus.Modules.Links.Controllers
{
	[Extension("Links")]
	[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
	public class AdminController : Controller
	{
		public const string MODULESETTING_CATEGORYLIST_ID = "links:categorylistid";
		public const string MODULESETTING_LAYOUT = "links:layout";

		private Context Context { get; }
		private PageManager PageManager { get; }
		private PageModuleManager PageModuleManager { get; }
		private LinksManager LinksManager { get; }
		private FileSystemManager FileSystemManager { get; }
		private ListManager ListManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }

		public AdminController(IWebHostEnvironment webHostEnvironment, Context Context, PageManager pageManager, PageModuleManager pageModuleManager, FileSystemManager fileSystemManager, LinksManager linksManager, ListManager listManager)
		{
			this.WebHostEnvironment = webHostEnvironment; 
			this.Context = Context;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
			this.LinksManager = linksManager;
			this.ListManager = listManager;			
		}

		//[HttpGet]
		//public ActionResult Index()
		//{
		//	return View("Viewer", BuildViewModel());
		//}

		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}


		[HttpPost]
		public ActionResult Create()
		{
			return View("Editor", BuildEditorViewModel(null, Guid.Empty));
		}

		[HttpGet]
		[HttpPost]
		public ActionResult Editor(ViewModels.Editor viewModel, Guid id)
		{
			return View("Editor", BuildEditorViewModel(viewModel, id));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SelectAnother(ViewModels.Editor viewModel)
		{
			viewModel.Link.LinkFile.File.ClearSelection();

			return View("Editor", BuildEditorViewModel(viewModel, Guid.Empty));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult MoveUp(ViewModels.Settings viewModel, Guid id)
		{
			this.LinksManager.MoveUp(this.Context.Module, id);
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult MoveDown(ViewModels.Settings viewModel, Guid id)
		{
			this.LinksManager.MoveDown(this.Context.Module, id);
			return View("Settings", BuildSettingsViewModel(viewModel));
		}


		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
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


		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SaveLink(ViewModels.Editor viewModel)
		{
			//switch (viewModel.Link.LinkType)
			//{
			//	case Models.LinkTypes.Url:
			//		viewModel.Link.LinkItem = viewModel.LinkUrl;
			//		break;

			//	case Models.LinkTypes.File:
			//		viewModel.Link.LinkItem = viewModel.LinkFile;
			//		break;

			//	case Models.LinkTypes.Page:
			//		viewModel.Link.LinkItem = viewModel.LinkPage;
			//		break;
			//}

			this.LinksManager.Save(this.Context.Module, viewModel.Link);

			viewModel.Message = "Changes Saved.";
			return Ok();
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_LAYOUT, viewModel.Layout);

			this.PageModuleManager.SaveSettings(this.Context.Module);

			viewModel.Message = "Changes Saved.";
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		//private ViewModels.Viewer BuildViewModel()
		//{
		//	ViewModels.Viewer viewModel = new();
		//	viewModel.Links = this.LinksManager.List(this.Context.Site, this.Context.Module);
		//	return viewModel;
		//}

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.CategoryList = this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
			viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");
			viewModel.Lists = this.ListManager.List(this.Context.Site);

			viewModel.ModuleId = this.Context.Module.Id;
			viewModel.Links = this.LinksManager.List(this.Context.Site, this.Context.Module);


			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{Constants.EXTENSIONS_ROUTE_PATH}\\Links\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}
			
			return viewModel;
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

			if (viewModel.Link == null)
			{
				if (id != Guid.Empty)
				{
					viewModel.Link = this.LinksManager.Get(this.Context.Site, id);
				}
				else
				{
					viewModel.Link = this.LinksManager.CreateNew();
				}
			}

			viewModel.CategoryList = this.ListManager.Get(this.Context.Module.ModuleSettings.Get<Guid>(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));

			switch (viewModel.Link.LinkType)
			{
				//case Models.LinkTypes.Url:
				//	viewModel.LinkUrl = viewModel.Link.LinkItem as LinkUrl;
				//	break;

				//case Models.LinkTypes.File:
				//	viewModel.LinkFile = viewModel.Link.LinkItem as LinkFile;
				//	break;

				case Models.LinkTypes.Page:
					//viewModel.LinkPage = viewModel.Link.LinkItem as LinkPage;
					viewModel.Pages = this.PageManager.List(this.Context.Site).Where(page => !page.Disabled).ToList();
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