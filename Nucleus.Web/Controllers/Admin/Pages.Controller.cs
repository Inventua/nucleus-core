using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Abstractions.Models.Permissions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.ViewFeatures;
using Nucleus.Extensions.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	public class PagesController : Controller
	{
		private ILogger<PagesController> Logger { get; }
		private Context Context { get; }
		private ILayoutManager LayoutManager { get; }
		private IContainerManager ContainerManager { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IRoleManager RoleManager { get; }
    private IFileSystemManager FileSystemManager { get; set; }
    private IUserManager UserManager { get; set; }

		public PagesController(
			Context context,
			ILogger<PagesController> logger,
			IPageManager pageManager,
			IPageModuleManager pageModuleManager,
      IFileSystemManager fileSystemManager,
			IRoleManager roleManager,
      IUserManager userManager,
			ILayoutManager layoutManager,
			IContainerManager containerManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
      this.UserManager = userManager;
			this.RoleManager = roleManager;
			this.LayoutManager = layoutManager;
			this.ContainerManager = containerManager;
      this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		/// Display the pages index
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildIndexViewModel());
		}

		/// <summary>
		/// Search pages and display the results
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		public async Task<ActionResult> Search(ViewModels.Admin.PageIndex viewModel)
		{
      User user = await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId());
      
      List<Role> roles = new() { this.Context.Site.AllUsersRole };
      
      if (HttpContext.User.IsSiteAdmin(this.Context.Site))
      {
        roles = null;
      }
      else if (HttpContext.User.IsAnonymous())
      {
        roles.Add(this.Context.Site.AnonymousUsersRole);
      }
      else
      {
        roles.AddRange((await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId()))?.Roles);
      }

      viewModel.SearchResults = await this.PageManager.Search(this.Context.Site, viewModel.SearchTerm, roles, viewModel.SearchResults);

			return View("SearchResults", viewModel);
		}

		/// <summary>
		/// Display the page editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> Editor(Guid id, ViewModels.Admin.PageEditor.PageEditorModes mode)
		{
			ViewModels.Admin.PageEditor viewModel;
			Page page;

			if (id == Guid.Empty)
			{
				page = await this.PageManager.CreateNew(this.Context.Site);
			}
			else
			{
				page = await this.PageManager.Get(id);
			}

			viewModel = await BuildPageEditorViewModel(page, null, true);

			viewModel.PageEditorMode = mode;
			
				
			return View("Editor", viewModel);
		}

    /// <summary>
		/// Display the page editor
		/// </summary>
		/// <returns></returns>
		[HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> Select(ViewModels.Admin.PageEditor viewModel)
    {
      Nucleus.Abstractions.Models.FileSystem.File selectedLinkFile = null;
      
      if (viewModel.SelectedLinkFile != null)
      {
        selectedLinkFile = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.SelectedLinkFile);
      }

      viewModel = await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true);
        
      if (selectedLinkFile != null)
      {
        viewModel.SelectedLinkFile = selectedLinkFile;
      }

      return View("Editor", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> SelectAnotherLinkFile(ViewModels.Admin.PageEditor viewModel)
    {
      viewModel.SelectedLinkFile.ClearSelection();
      return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true));
    }

    [HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> DeletePage(ViewModels.Admin.PageEditor viewModel)
		{
			await this.PageManager.Delete(viewModel.Page);

			return View("Index", await BuildIndexViewModel());
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> AddPageRoute(ViewModels.Admin.PageEditor viewModel)
		{
			viewModel.Page.Routes.Add(new PageRoute());

			return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true));
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
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> RemovePageRoute(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			
			foreach (PageRoute route in viewModel.Page.Routes)
			{
				if (route.Id == id)
				{
					viewModel.Page.Routes.Remove(route);
					break;
				}
			}

			return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> RemovePagePermissionRole(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			viewModel.Page.Permissions = await RebuildPermissions(viewModel.PagePermissions);

			foreach (Permission permission in viewModel.Page.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Page.Permissions.Remove(permission);
				}
			}

			viewModel.PagePermissions = viewModel.Page.Permissions.ToPermissionsList(this.Context.Site);
			viewModel.DisableCopy = true;

			return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, false));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> Save(ViewModels.Admin.PageEditor viewModel)
		{
			Boolean isNew = (viewModel.Page.Id == Guid.Empty);

      // prevent validation errors when a link file is not selected
      ControllerContext.ModelState.Remove<ViewModels.Admin.PageEditor>(viewModel => viewModel.SelectedLinkFile.Id);

			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			viewModel.Page.Permissions = await RebuildPermissions(viewModel.PagePermissions);
      viewModel.Page.LinkFileId = viewModel.SelectedLinkFile?.Id;

      await this.PageManager.Save(this.Context.Site, viewModel.Page);

			if (viewModel.PageEditorMode != ViewModels.Admin.PageEditor.PageEditorModes.Default)
			{
				if (viewModel.Page.DefaultPageRoute() != null)
				{
					string location = Url.GetAbsoluteUri(viewModel.Page.DefaultPageRoute().Path).ToString();
					//ControllerContext.HttpContext.Response.Headers.Add("X-Location", location);
					ControllerContext.HttpContext.Response.Headers.Append("X-Location-Target", "_top");
					//return StatusCode((int)System.Net.HttpStatusCode.Found);
          return ControllerContext.HttpContext.NucleusRedirect(location);
        }
				else
				{
					return Ok();
				}
			}
			else
			{
				return View("Index", await BuildIndexViewModel(viewModel.Page.Id, isNew));
			}
		}

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    public async Task<ActionResult> CopyPermissionsReplaceAll(ViewModels.Admin.PageEditor viewModel)
    {
      if (await this.PageManager.CopyPermissionsToDescendants(this.Context.Site, viewModel.Page, User, CopyPermissionOperation.Replace))
      {
				return Json(new { Title = "Copy Permissions to Descendants", Message = "Permissions were copied successfully.", Icon = "alert" });
			}
			else
      {
        return BadRequest();
      }
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    public async Task<ActionResult> CopyPermissionsMerge(ViewModels.Admin.PageEditor viewModel)
    {
      if (await this.PageManager.CopyPermissionsToDescendants(this.Context.Site, viewModel.Page, User, CopyPermissionOperation.Merge))
      {
				return Json(new { Title = "Copy Permissions to Descendants", Message = "Permissions were copied successfully.", Icon = "alert" });
			}
			else
      {
        return BadRequest();
      }
    }


    [HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		public async Task<ActionResult> MovePageDown(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			await this.PageManager.MoveDown(this.Context.Site, id);

			return View("Index", await BuildIndexViewModel(id, true));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		public async Task<ActionResult> MovePageUp(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			await this.PageManager.MoveUp(this.Context.Site, id);

			return View("Index", await BuildIndexViewModel(id, true));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> SaveModule(ViewModels.Admin.PageEditor viewModel)
		{
			await this.PageModuleManager.Save(viewModel.Page, viewModel.Module);
			return View("Editor", await BuildPageEditorViewModel(await this.PageManager.Get(viewModel.Page.Id), null, true));
		}

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> MoveModuleTo(Guid? beforeModuleId, string pane)
    {
      PageModule beforeModule = null;

      if (beforeModuleId.HasValue && beforeModuleId.Value != Guid.Empty)
      {
        beforeModule = await this.PageModuleManager.Get(beforeModuleId.Value);
      }

      await this.PageModuleManager.MoveTo(this.Context.Page, this.Context.Module, pane, beforeModule);
      return Ok();
    }


    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> UpdateModuleTitle(string value)
    {      
      this.Context.Module.Title = value;
      await this.PageModuleManager.Save(this.Context.Page, this.Context.Module);
      return Ok();
    }

    [HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> RemoveModulePermissionRole(ViewModels.Admin.PageEditor viewModel, Guid id)
		{
			viewModel.Module.Permissions = await RebuildPermissions(viewModel.ModulePermissions);

			foreach (Permission permission in viewModel.Module.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Module.Permissions.Remove(permission);
				}
			}

			viewModel.ModulePermissions = viewModel.Module.Permissions.ToPermissionsList(this.Context.Site);

			return View("ModuleCommonSettings", await BuildPageViewModel(viewModel.Page, viewModel.Module, viewModel.ModulePermissions, false));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> AddModule(ViewModels.Admin.PageEditor viewModel)
		{
			PageModule module = await this.PageModuleManager.CreateNew(this.Context.Site, viewModel.Page);
			viewModel = await BuildPageViewModel(viewModel.Page, module, null, true);
			
			return View("ModuleCommonSettings", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> EditModuleCommonSettings(ViewModels.Admin.PageEditor viewModel, Guid mid, ViewModels.Admin.PageEditor.PageEditorModes mode)
		{
			PageModule module = await this.PageModuleManager.Get(mid);
			viewModel = await BuildPageViewModel(viewModel.Page, module, null, true);
			viewModel.PageEditorMode = mode;

			if (mode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)
			{
				viewModel.UseLayout = "_PopupEditor";				
			}
						
			return View("ModuleCommonSettings", viewModel);			
		}

		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> EditModuleCommonSettings(Guid mid, ViewModels.Admin.PageEditor.PageEditorModes mode)
		{
			ViewModels.Admin.PageEditor viewModel;

			PageModule module = await this.PageModuleManager.Get(mid);
			Page page = await this.PageManager.Get(module.PageId);

			viewModel = await BuildPageViewModel(page, module, null, true);
			viewModel.PageEditorMode = mode;

			if (mode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)
			{
				viewModel.UseLayout = "_PopupEditor";
			}

			return View("ModuleCommonSettings", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> CreateModule(ViewModels.Admin.PageEditor viewModel)
		{
			if (viewModel.Module.ModuleDefinition == null || viewModel.Module.ModuleDefinition.Id == Guid.Empty)
			{
				return BadRequest("No module type selected.");
			}

			if (viewModel.Page.Id == Guid.Empty)
			{
				return BadRequest("You must save your new page before adding a module.");
			}

			await this.PageModuleManager.Save(viewModel.Page, viewModel.Module);

			viewModel.Module.Permissions = await RebuildPermissions(viewModel.ModulePermissions);
			await this.PageModuleManager.SavePermissions(viewModel.Page, viewModel.Module);

			viewModel = await BuildPageEditorViewModel(viewModel.Page, null, true);

			return View("_PageModules", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> SaveModuleCommonSettings(ViewModels.Admin.PageEditor viewModel)
		{
			if (viewModel.Module.ModuleDefinition == null || viewModel.Module.ModuleDefinition.Id == Guid.Empty)
			{
				return BadRequest("No module type selected.");
			}

			if (viewModel.Page.Id == Guid.Empty)
			{
				return BadRequest("You must save your new page before adding a module.");
			}

			viewModel.Module.Permissions = await RebuildPermissions(viewModel.ModulePermissions);

			await this.PageModuleManager.SavePermissions(viewModel.Page, viewModel.Module);
			await this.PageModuleManager.Save(viewModel.Page, viewModel.Module);

			if (viewModel.PageEditorMode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)
			{
				return Ok();
			}
			else
			{
				viewModel = await BuildPageEditorViewModel(viewModel.Page, null, false);
				return View("_PageModules", viewModel);
			}
		}

		[HttpPost]
		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> EditModule(Guid mid, ViewModels.Admin.PageEditor.PageEditorModes mode)
		{
			ViewModels.Admin.PageModuleEditor viewModel = await BuildPageModuleViewModel(mid);
			

			if (mode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)
			{
				viewModel.UseLayout = "_PopupEditor";
			}
			
			return View("ModuleEditor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> EditModulePermissions(Guid mid)
		{
			ViewModels.Admin.PageEditor viewModel;

			PageModule module = await this.PageModuleManager.Get(mid);
			module.Permissions = await this.PageModuleManager.ListPermissions(module);
			viewModel = await BuildPageViewModel(await this.PageManager.Get(module), module, null, true);

			return View("ModulePermissionsEditor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> AddModulePermissionRole(ViewModels.Admin.PageEditor viewModel, [FromQuery ]Boolean Standalone)
		{
			if (viewModel.SelectedModuleRoleId != Guid.Empty)
			{				
				viewModel.Module.Permissions = await RebuildPermissions (viewModel.ModulePermissions);
				await this.PageModuleManager.CreatePermissions(this.Context.Site, viewModel.Module, await this.RoleManager.Get(viewModel.SelectedModuleRoleId));
			}
			
			viewModel.ModulePermissions = viewModel.Module.Permissions.ToPermissionsList(this.Context.Site);

			viewModel = await BuildPageViewModel(viewModel.Page, viewModel.Module, viewModel.ModulePermissions, false);

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
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> SaveModulePermissions(ViewModels.Admin.PageEditor viewModel, Guid mid)
		{
			PageModule module = await this.PageModuleManager.Get(mid);
			module.Permissions = await RebuildPermissions(viewModel.ModulePermissions);

			await this.PageModuleManager.SavePermissions(viewModel.Page, module);

			return Ok();
		}

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> DeleteModule(ViewModels.Admin.PageEditor viewModel, Guid mid)
    {
      await this.PageModuleManager.Delete(mid);

      return View("_PageModules", await BuildPageEditorViewModel(viewModel.Page, null, true));
    }

		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public ActionResult DeleteModule(Guid mid)
		{
			// this action exists to return an error if a user tries to enter the DeleteModule url into their browser
			return BadRequest();
		}

		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public ActionResult DeletePageModuleInline(Guid mid)
		{
			// this action exists to return an error if a user tries to enter the DeletePageModuleInline url into their browser
			return BadRequest();
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> DeletePageModuleInline(ViewModels.Admin.PageEditor viewModel, Guid mid)
		{
			await this.PageModuleManager.Delete(mid);
			
			return Ok();			
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> MoveModuleDown(ViewModels.Admin.PageEditor viewModel, Guid mid )
		{
			await this.PageModuleManager.MoveDown(viewModel.Page, mid);

			viewModel = await BuildPageEditorViewModel(viewModel.Page, null, true);

			return View("_PageModules", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> MoveModuleUp(ViewModels.Admin.PageEditor viewModel, Guid mid)
		{
			await this.PageModuleManager.MoveUp(viewModel.Page, mid);

			viewModel = await BuildPageEditorViewModel(viewModel.Page, null, true);

			return View("_PageModules", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
		public async Task<ActionResult> AddPagePermissionRole(ViewModels.Admin.PageEditor viewModel)
		{			
			if (viewModel.SelectedPageRoleId != Guid.Empty)
			{
				Role role = await this.RoleManager.Get(viewModel.SelectedPageRoleId);
				viewModel.Page.Permissions = await RebuildPermissions(viewModel.PagePermissions);

				await this.PageManager.CreatePermissions(this.Context.Site, viewModel.Page, role);
			}

			viewModel.PagePermissions = viewModel.Page.Permissions.ToPermissionsList(this.Context.Site);
			viewModel.DisableCopy = true;

			return View("Editor", await BuildPageEditorViewModel (viewModel.Page, viewModel.PagePermissions, false));
		}

		[HttpGet]
		public async Task<ActionResult> GetChildPages(Guid id)
		{
			ViewModels.Admin.PageIndexPartial viewModel = new();

			viewModel.FromPage = await this.PageManager.Get(id);

			viewModel.Pages = await this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					await this.PageManager.Get(id),
					ControllerContext.HttpContext.User,
					1
				);

			return View("PageMenu", viewModel);
		}

		private async Task<ViewModels.Admin.PageIndex> BuildIndexViewModel(Guid pageId, Boolean openPage)
		{
			ViewModels.Admin.PageIndex viewModel = new();

			viewModel.Pages = await this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					null,
					ControllerContext.HttpContext.User,
					pageId
				);

			viewModel.PageId = pageId;
			viewModel.OpenPage = openPage;
			return viewModel;
		}

		private async Task<ViewModels.Admin.PageIndex> BuildIndexViewModel()
		{
			ViewModels.Admin.PageIndex viewModel = new();

			viewModel.Pages = await this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					null,
					ControllerContext.HttpContext.User,
					0
				);

			return viewModel;
		}

		private async Task<ViewModels.Admin.PageEditor> BuildPageViewModel(Page page, PageModule module, PermissionsList modulePermissions, Boolean getPermissions)
		{
			ViewModels.Admin.PageEditor viewModel = await BuildPageEditorViewModel(page, null, false);
			viewModel.Module = module;
			viewModel.ModulePermissions = modulePermissions;

			viewModel.AvailableModuleRoles = await GetAvailableRoles(viewModel.Module?.Permissions);
			viewModel.ModulePermissionTypes = await this.PageModuleManager.ListModulePermissionTypes();

      // Set modules with an invalid pane to "Missing Pane"
      if (!viewModel.AvailablePanes.Contains(viewModel.Module.Pane))
      {
        viewModel.Module.Pane = "None";
      }

      if (getPermissions)
			{
				// read permissions from the database to initialize the viewModel
				module.Permissions = await this.PageModuleManager.ListPermissions(module);
				viewModel.ModulePermissions = module.Permissions.ToPermissionsList(this.Context.Site);
			}
			else
			{
				// re-populate missing data from existing viewModel permissions
				await RebuildPermissions (viewModel.ModulePermissions);
			}


			return viewModel;
		}

		private async Task<ViewModels.Admin.PageModuleEditor> BuildPageModuleViewModel(Guid mid)
		{
			ViewModels.Admin.PageModuleEditor viewModel = new()
			{
				Module = await this.PageModuleManager.Get(mid)
			};

      return viewModel;
		}

		private async Task<ViewModels.Admin.PageEditor> BuildPageEditorViewModel(Page page, PermissionsList pagePermissions, Boolean getPermissions)
		{
			ViewModels.Admin.PageEditor viewModel = new();

			viewModel.Page = page;
			viewModel.PagePermissions = pagePermissions;

      // if the page was populated by MVC, it won't have any modules.  Checking Modules.Any() before calling .ListModules is a small improvement in performance
      if (!viewModel.Page.Modules.Any())
      {
        viewModel.Page.Modules = await this.PageManager.ListModules(viewModel.Page);
      }

			viewModel.ParentPageMenu = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, page.ParentId);
      viewModel.LinkPageMenu = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, viewModel.Page.LinkPageId);

      viewModel.AvailableModules = await GetAvailableModules();
			viewModel.AvailablePageRoles = await GetAvailableRoles(viewModel.Page?.Permissions);			
			viewModel.PagePermissionTypes = await this.PageManager.ListPagePermissionTypes();

      if (viewModel.Page.LinkFileId.HasValue)
      {
        try
        {
          viewModel.SelectedLinkFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.Page.LinkFileId.Value);
        }
        catch (FileNotFoundException)
        {
          viewModel.SelectedLinkFile = null;
        }
      }
      
      if (getPermissions)
			{
				// read permissions from the database to initialize the viewModel
				page.Permissions = await this.PageManager.ListPermissions(page);
				viewModel.PagePermissions = page.Permissions.ToPermissionsList(this.Context.Site);
			}
			else
			{
				// re-populate missing data from existing viewModel permissions
				await RebuildPermissions(viewModel.PagePermissions);
			}

			viewModel.CanDeletePage = User.HasEditPermission(this.Context.Site, viewModel.Page);
			
			// re-read the selected layout (viewmodel will only contain ID, and we need RelativePath for the call to ListLayoutPanes)
			if (viewModel.Page.LayoutDefinition != null)
			{
				viewModel.Page.LayoutDefinition = await this.LayoutManager.Get(viewModel.Page.LayoutDefinition.Id);
			}

			viewModel.Layouts = (await this.LayoutManager.List()).InsertDefaultListItem();
			viewModel.Containers = (await this.ContainerManager.List()).InsertDefaultListItem();
			
			viewModel.AvailablePanes = (await this.LayoutManager.ListLayoutPanes(viewModel.Page.LayoutDefinition ?? this.Context.Site.DefaultLayoutDefinition)).Append("None");

			// Set modules with an invalid pane to "Missing Pane"
			foreach(PageModule moduleMissingPane in viewModel.Page.Modules.Where(module => !viewModel.AvailablePanes.Contains(module.Pane)))
			{
				moduleMissingPane.Pane = "None";
			}

			return viewModel;
		}

		private async Task<IEnumerable<SelectListItem>> GetAvailableModules()
		{

			IEnumerable<ModuleDefinition> availableModules = await this.PageModuleManager.ListModuleDefinitions();
			IEnumerable<string> moduleCategories = availableModules
				.SelectMany(module => String.IsNullOrEmpty(module.Categories) ? new string[] { "" } : module.Categories.Split(',')
				.Select(category => category))
				.Where(category => !String.IsNullOrEmpty(category))
				.Distinct()
				.OrderBy(name => name);

			Dictionary<string, SelectListGroup> groups = moduleCategories.ToDictionary(name => name, name => new SelectListGroup() { Name = name });

			return availableModules.Select(module => new SelectListItem(module.FriendlyName, module.Id.ToString())
			{
				Group = groups.Where(group => (string.IsNullOrEmpty(module.Categories) ? new string[] { "" } : module.Categories.Split(',')).Contains(group.Key)).FirstOrDefault().Value
			})
			.OrderBy(selectListItem=>selectListItem.Group?.Name);
		}

		private async Task<IEnumerable<SelectListItem>> GetAvailableRoles(List<Permission> existingPermissions)
		{
			IEnumerable<Role> availableRoles = (await this.RoleManager.List(this.Context.Site))
				.Where
				(
					role => role.Id != this.Context.Site.AdministratorsRole?.Id && (existingPermissions == null || !existingPermissions.Where(item => item.Role.Id == role.Id).Any())
				)
				.OrderBy(role => role.Name);

			IEnumerable<string> roleGroups = availableRoles
				.Where(role => role.RoleGroup != null)
				.Select(role => role.RoleGroup.Name)
				.Distinct()
				.OrderBy(name => name);

			Dictionary<string, SelectListGroup> groups = roleGroups.ToDictionary(name => name, name => new SelectListGroup() { Name = name });

			return availableRoles.Select(role => new SelectListItem(role.Name, role.Id.ToString())
			{
				Group = groups.Where(group => role.RoleGroup != null && role.RoleGroup.Name == group.Key).FirstOrDefault().Value
			})
			.OrderBy(selectListItem => selectListItem.Group?.Name);
		}


		private async Task<List<Permission>> RebuildPermissions(PermissionsList permissions)
		{
			if (permissions == null) return null;

			foreach (KeyValuePair<Guid, PermissionsListItem> rolePermissions in permissions)
			{
				foreach (Permission permission in rolePermissions.Value.Permissions)
				{
					permission.Role = await this.RoleManager.Get(rolePermissions.Key);

					rolePermissions.Value.Role = permission.Role;
				}
			}

			return permissions.ToList();
		}
	}
}
