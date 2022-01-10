using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Core;
using Nucleus.Abstractions.Models.Permissions;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	public class PagesController : Controller
	{
		private ILogger<PagesController> Logger { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }
		private Context Context { get; }
		private LayoutManager LayoutManager { get; }
		private ContainerManager ContainerManager { get; }
		private PageManager PageManager { get; }
		private PageModuleManager PageModuleManager { get; }
		private RoleManager RoleManager { get; }

		public PagesController(
			Context context,
			ILogger<PagesController> logger,
			IWebHostEnvironment webHostEnvironment,
			PageManager pageManager,
			PageModuleManager pageModuleManager,
			RoleManager roleManager,
			LayoutManager layoutManager,
			ContainerManager containerManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.WebHostEnvironment = webHostEnvironment;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.RoleManager = roleManager;
			this.LayoutManager = layoutManager;
			this.ContainerManager = containerManager;
		}

		/// <summary>
		/// Display the pages index
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult Index()
		{
			return View("Index", BuildIndexViewModel());
		}

		/// <summary>
		/// Display the pages index
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult Search(ViewModels.Admin.PageIndex viewModel)
		{
			
			viewModel.SearchResults = this.PageManager.Search(this.Context.Site, viewModel.SearchTerm);

			return View("SearchResults", viewModel);
		}

		/// <summary>
		/// Display the page editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult Editor(Guid id)
		{
			ViewModels.Admin.PageEditor viewModel;
			Page page;

			if (id == Guid.Empty)
			{
				page = this.PageManager.CreateNew(this.Context.Site);
			}
			else
			{
				page = this.PageManager.Get(id);
			}

			viewModel = BuildPageEditorViewModel(page, null, true);

			return View("Editor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult DeletePage(ViewModels.Admin.PageEditor viewModel)
		{
			this.PageManager.Delete(viewModel.Page);

			return View("Index", BuildIndexViewModel());
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult AddPageRoute(ViewModels.Admin.PageEditor viewModel)
		{
			viewModel.Page.Routes.Add(new PageRoute());

			return View("Editor", BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true));
		}

		/// <summary>
		/// Remove the route specified by id from the currently selected page's routes.  
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <remarks>
		/// If a route with the specified id is not present in the currently selected page's routes, no action is taken and no exception is 
		/// generated.  The "delete" occurs within the viewModel only - the Save action must be called in order to commit the delete operation
		/// to the database.
		/// </remarks>
		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult DeletePageRoute(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			
			foreach (PageRoute route in viewModel.Page.Routes)
			{
				if (route.Id == id)
				{
					viewModel.Page.Routes.Remove(route);
					break;
				}
			}

			return View("Editor", BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult DeletePagePermissionRole(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			viewModel.Page.Permissions = RebuildPermissions(viewModel.PagePermissions);

			foreach (Permission permission in viewModel.Page.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Page.Permissions.Remove(permission);
				}
			}

			viewModel.PagePermissions = viewModel.Page.Permissions.ToPermissionsList();

			return View("Editor", BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, false));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult DeleteModulePermissionRole(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			viewModel.Module.Permissions = RebuildPermissions(viewModel.ModulePermissions);

			foreach (Permission permission in viewModel.Module.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Module.Permissions.Remove(permission);
				}
			}

			viewModel.ModulePermissions = viewModel.Module.Permissions.ToPermissionsList();

			return View("ModuleCommonSettings", BuildPageViewModel(viewModel.Page, viewModel.Module, viewModel.ModulePermissions, false));
		}


		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult Save(ViewModels.Admin.PageEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			viewModel.Page.Permissions = RebuildPermissions(viewModel.PagePermissions);

			this.PageManager.Save(this.Context.Site, viewModel.Page);
			return View("Index", BuildIndexViewModel());			
		}


		[HttpPost]
		public ActionResult SaveModule(ViewModels.Admin.PageEditor viewModel)
		{
			this.PageModuleManager.Save(viewModel.Page, viewModel.Module);
			return View("Editor", BuildPageEditorViewModel(this.PageManager.Get(viewModel.Page.Id), null, true));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult AddModule(ViewModels.Admin.PageEditor viewModel)
		{
			PageModule module = this.PageModuleManager.CreateNew(this.Context.Site);
			viewModel = BuildPageViewModel(viewModel.Page, module, null, true);
			
			return View("ModuleCommonSettings", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult EditModuleCommonSettings(ViewModels.Admin.PageEditor viewModel, Guid mid, Boolean standalone)
		{
			PageModule module = this.PageModuleManager.Get(mid);
			viewModel = BuildPageViewModel(viewModel.Page, module, null, true);

			if (standalone)
			{
				viewModel.UseLayout = "_PopupEditor";
			}
						
			return View("ModuleCommonSettings", viewModel);			
		}

		[HttpGet]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult EditModuleCommonSettings(Guid mid, Boolean standalone)
		{
			ViewModels.Admin.PageEditor viewModel;

			PageModule module = this.PageModuleManager.Get(mid);
			Page page = this.PageManager.Get(this.PageModuleManager.GetPageId(module));

			viewModel = BuildPageViewModel(page, module, null, true);
			
			if (standalone)
			{
				viewModel.UseLayout = "_PopupEditor";
			}

			return View("ModuleCommonSettings", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult CreateModule(ViewModels.Admin.PageEditor viewModel)
		{
			if (viewModel.Module.ModuleDefinition == null || viewModel.Module.ModuleDefinition.Id == Guid.Empty)
			{
				return BadRequest("No module type selected.");
			}

			if (viewModel.Page.Id == Guid.Empty)
			{
				return BadRequest("You must save your new page before adding a module.");
			}

			this.PageModuleManager.Save(viewModel.Page, viewModel.Module);

			viewModel.Module.Permissions = RebuildPermissions(viewModel.ModulePermissions);
			this.PageModuleManager.SavePermissions(viewModel.Module);

			viewModel = BuildPageEditorViewModel(viewModel.Page, null, true);

			return View("Editor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult SaveModuleCommonSettings(ViewModels.Admin.PageEditor viewModel)
		{
			if (viewModel.Module.ModuleDefinition == null || viewModel.Module.ModuleDefinition.Id == Guid.Empty)
			{
				return BadRequest("No module type selected.");
			}

			if (viewModel.Page.Id == Guid.Empty)
			{
				return BadRequest("You must save your new page before adding a module.");
			}

			viewModel.Module.Permissions = RebuildPermissions(viewModel.ModulePermissions);
			this.PageModuleManager.SavePermissions(viewModel.Module);

			this.PageModuleManager.Save(viewModel.Page, viewModel.Module);
						
			return Ok();
		}

		[HttpPost]
		[HttpGet]
		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		public ActionResult EditModule(Guid mid, Boolean standalone)
		{
			ViewModels.Admin.PageModuleEditor viewModel = BuildPageModuleViewModel(mid);
			//viewModel = BuildViewModel(viewModel.Page, null, true);

			if (standalone)
			{
				viewModel.UseLayout = "_PopupEditor";
				viewModel.RenderHeader = false;
			}
			else
			{
				viewModel.RenderHeader = true;
			}

			return View("ModuleEditor", viewModel);

		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult EditModulePermissions(Guid mid)
		{
			ViewModels.Admin.PageEditor viewModel;

			PageModule module = this.PageModuleManager.Get(mid);
			module.Permissions = this.PageModuleManager.ListPermissions(module);
			viewModel = BuildPageViewModel(this.PageManager.Get(module), module, null, true);

			return View("ModulePermissionsEditor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult AddModulePermissionRole(ViewModels.Admin.PageEditor viewModel, [FromQuery ]Boolean Standalone)
		{
			if (viewModel.SelectedModuleRoleId != Guid.Empty)
			{				
				viewModel.Module.Permissions = RebuildPermissions(viewModel.ModulePermissions);
				this.PageModuleManager.CreatePermissions(viewModel.Module, this.RoleManager.Get(viewModel.SelectedModuleRoleId));
			}
			
			viewModel.ModulePermissions = viewModel.Module.Permissions.ToPermissionsList();

			viewModel = BuildPageViewModel(viewModel.Page, viewModel.Module, viewModel.ModulePermissions, false);

			if (Standalone)
			{
				return View("ModulePermissionsEditor", viewModel);
			}
			else
			{
				return View("ModuleCommonSettings", viewModel);
			}
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult SaveModulePermissions(ViewModels.Admin.PageEditor viewModel, Guid mid)
		{
			PageModule module = this.PageModuleManager.Get(mid);
			module.Permissions = RebuildPermissions(viewModel.ModulePermissions);

			this.PageModuleManager.SavePermissions(module);

			return Ok();
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult DeleteModule(ViewModels.Admin.PageEditor viewModel, Guid mid)
		{
			this.PageModuleManager.Delete(mid);

			return View("Editor", BuildPageEditorViewModel(viewModel.Page, null, true));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult DeletePageModule(ViewModels.Admin.PageEditor viewModel, Guid mid)
		{
			this.PageModuleManager.Delete(mid);

			return Ok();
		}


		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult MovePageDown(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			this.PageManager.MoveDown(this.Context.Site, id);

			viewModel = BuildPageEditorViewModel(viewModel.Page, null, true);

			return View("Index", BuildIndexViewModel(id));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult MovePageUp(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			this.PageManager.MoveUp(this.Context.Site, id);

			viewModel = BuildPageEditorViewModel(viewModel.Page, null, true);

			return View("Index", BuildIndexViewModel(id));
		}


		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult MoveModuleDown(ViewModels.Admin.PageEditor viewModel, Guid mid )
		{
			this.PageModuleManager.MoveDown(mid);

			viewModel = BuildPageEditorViewModel(viewModel.Page, null, true);

			return View("Editor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult MoveModuleUp(ViewModels.Admin.PageEditor viewModel, Guid mid)
		{
			this.PageModuleManager.MoveUp(mid);

			viewModel = BuildPageEditorViewModel(viewModel.Page, null, true);

			return View("Editor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
		public ActionResult AddPagePermissionRole(ViewModels.Admin.PageEditor viewModel)
		{			
			if (viewModel.SelectedPageRoleId != Guid.Empty)
			{
				Role role = this.RoleManager.Get(viewModel.SelectedPageRoleId);
				viewModel.Page.Permissions = RebuildPermissions(viewModel.PagePermissions);

				this.PageManager.CreatePermissions(viewModel.Page, role);
			}

			viewModel.PagePermissions = viewModel.Page.Permissions.ToPermissionsList();

			return View("Editor", BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, false));
		}

		private ViewModels.Admin.PageIndex BuildIndexViewModel(Guid pageId)
		{
			ViewModels.Admin.PageIndex viewModel = BuildIndexViewModel();

			viewModel.PageId = pageId;
			return viewModel;
		}

		private ViewModels.Admin.PageIndex BuildIndexViewModel()
		{
			ViewModels.Admin.PageIndex viewModel = new();

			viewModel.Pages = this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					ControllerContext.HttpContext.User
				);

			//viewModel.Pages = this.PageManager.List(this.Context.Site);

			return viewModel;
		}

		private ViewModels.Admin.PageEditor BuildPageViewModel(Page page, PageModule module, PermissionsList modulePermissions, Boolean getPermissions)
		{
			ViewModels.Admin.PageEditor viewModel = BuildPageEditorViewModel(page, null, false);
			viewModel.Module = module;
			viewModel.ModulePermissions = modulePermissions;

			viewModel.AvailableModuleRoles = this.RoleManager.List(this.Context.Site).Where
			(
				role => role.Id != this.Context.Site.AdministratorsRole?.Id && !viewModel.Module.Permissions.Where(permission => permission.Role.Id == role.Id).Any()
			);

			viewModel.ModulePermissionTypes = this.PageModuleManager.ListModulePermissionTypes();

			if (getPermissions)
			{
				// read permissions from the database to initialize the viewModel
				module.Permissions = this.PageModuleManager.ListPermissions(module);
				viewModel.ModulePermissions = module.Permissions.ToPermissionsList();
			}
			else
			{
				// re-populate missing data from existing viewModel permissions
				RebuildPermissions(viewModel.ModulePermissions);
			}


			return viewModel;
		}

		private ViewModels.Admin.PageModuleEditor BuildPageModuleViewModel(Guid mid)
		{
			ViewModels.Admin.PageModuleEditor viewModel = new()
			{
				Module = this.PageModuleManager.Get(mid)
			};

			return viewModel;
		}

		private ViewModels.Admin.PageEditor BuildPageEditorViewModel(Page page, PermissionsList pagePermissions, Boolean getPermissions)
		{
			ViewModels.Admin.PageEditor viewModel = new();

			viewModel.Page = page;
			viewModel.PagePermissions = pagePermissions;

			viewModel.Page.Modules = this.PageManager.ListModules(viewModel.Page);

			viewModel.Pages = this.PageManager.List(this.Context.Site).Where((page) => page.Id != viewModel.Page.Id);
			viewModel.AvailableModules = this.PageModuleManager.ListModuleDefinitions();

			viewModel.AvailablePageRoles = this.RoleManager.List(this.Context.Site).Where
			(
				role => role.Id != this.Context.Site.AdministratorsRole?.Id && !viewModel.Page.Permissions.Where(item => item.Role.Id == role.Id).Any()
			);
			
			viewModel.PagePermissionTypes = this.PageManager.ListPagePermissionTypes();
						
			if (getPermissions)
			{
				// read permissions from the database to initialize the viewModel
				page.Permissions = this.PageManager.ListPermissions(page);
				viewModel.PagePermissions = page.Permissions.ToPermissionsList();
			}
			else
			{
				// re-populate missing data from existing viewModel permissions
				RebuildPermissions(viewModel.PagePermissions);
			}

			// re-read the selected layout (viewmodel will only contain ID, and we need RelativePath for the call to ListLayoutPanes)
			if (viewModel.Page.Layout != null)
			{
				viewModel.Page.Layout = this.LayoutManager.Get(viewModel.Page.Layout.Id);
			}

			viewModel.Layouts = this.LayoutManager.List().InsertDefaultListItem();
			viewModel.Containers = this.ContainerManager.List().InsertDefaultListItem();
			
			viewModel.AvailablePanes = this.LayoutManager.ListLayoutPanes(viewModel.Page.Layout);

			return viewModel;
		}
		

		//private List<Permission> ConvertPermissions(PermissionsList permissions)
		//{
		//	if (permissions == null) return null;

		//	RebuildPermissions(permissions);
			
		//	return permissions.ToList();
		//}

		private List<Permission> RebuildPermissions(PermissionsList permissions)
		{
			if (permissions == null) return null;

			foreach (KeyValuePair<Guid, PermissionsListItem> rolePermissions in permissions)
			{
				foreach (Permission permission in rolePermissions.Value.Permissions)
				{
					permission.Role = this.RoleManager.Get(rolePermissions.Key);

					rolePermissions.Value.Role = permission.Role;
				}
			}

			return permissions.ToList();
		}
	}
}
