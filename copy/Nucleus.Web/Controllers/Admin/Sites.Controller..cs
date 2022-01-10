using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Core;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]	
	public class SitesController : Controller
	{
		private ILogger<SitesController> Logger { get; }
		private SiteManager SiteManager{ get; }
		private RoleManager RoleManager { get; }
		private PageManager PageManager { get; }
		private MailTemplateManager MailTemplateManager { get; }
		private FileSystemManager FileSystemManager { get; }
		private Context Context { get; }
		private LayoutManager LayoutManager { get; }

		public SitesController(Context context, ILogger<SitesController> logger, SiteManager siteManager, PageManager pageManager, MailTemplateManager mailTemplateManager,  RoleManager roleManager, LayoutManager layoutManager, FileSystemManager fileSystemManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.SiteManager = siteManager;
			this.PageManager = pageManager;
			this.MailTemplateManager = mailTemplateManager;
			this.RoleManager = roleManager;
			this.LayoutManager = layoutManager;
			this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		/// Display the site editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY)]
		public ActionResult Index()
		{
			return View("Index", BuildViewModel());
		}

		/// <summary>
		/// Display the Site editor for the "current" site
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult EditCurrentSite()
		{
			ViewModels.Admin.SiteEditor viewModel;
						
			viewModel = BuildViewModel(this.Context.Site, true);
			viewModel.Site.Aliases = this.SiteManager.ListAliases(viewModel.Site);
			

			return View("Editor", viewModel);
		}

		/// <summary>
		/// Display the Site editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY)]
		public ActionResult Editor(Guid id)
		{
			ViewModels.Admin.SiteEditor viewModel;
						
			viewModel = BuildViewModel(id == Guid.Empty ? this.SiteManager.CreateNew() : this.SiteManager.Get(id), false);
			viewModel.Site.Aliases = this.SiteManager.ListAliases(viewModel.Site);
			

			return View("Editor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY)]
		public ActionResult AddSite()
		{
			return View("Editor", BuildViewModel(new Site(), false));
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY)]
		public ActionResult DeleteSite(ViewModels.Admin.SiteEditor viewModel)
		{
			this.SiteManager.Delete(viewModel.Site);
			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult AddAlias(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel = BuildViewModel(this.SiteManager.Get(viewModel.Site.Id), viewModel.IsCurrentSiteEditor);
			viewModel.Alias = new();
			
			return View("AliasEditor", viewModel);
		}

		[HttpPost]
		public ActionResult EditAlias(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{			
			viewModel = BuildViewModel(this.SiteManager.Get(viewModel.Site.Id), viewModel.IsCurrentSiteEditor);
			viewModel.Alias = this.SiteManager.GetAlias(id);
			
			return View("AliasEditor", viewModel);
		}


		[HttpPost]
		public ActionResult SaveAlias(ViewModels.Admin.SiteEditor viewModel)
		{			
			this.SiteManager.SaveAlias(viewModel.Site, viewModel.Alias);
			viewModel = BuildViewModel(this.SiteManager.Get(viewModel.Site.Id), viewModel.IsCurrentSiteEditor);
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult DeleteAlias(Guid id, ViewModels.Admin.SiteEditor viewModel)
		{			
			this.SiteManager.DeleteAlias(viewModel.Site, id);
			viewModel = BuildViewModel(this.SiteManager.Get(viewModel.Site.Id), viewModel.IsCurrentSiteEditor);				
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddUserProfileProperty(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel = BuildViewModel(this.SiteManager.Get(viewModel.Site.Id), viewModel.IsCurrentSiteEditor);
			viewModel.Property = new();

			return View("UserProfilePropertyEditor", viewModel);
		}	
			
		[HttpPost]
		public ActionResult EditUserProfileProperty(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			viewModel = BuildViewModel(this.SiteManager.Get(viewModel.Site.Id), viewModel.IsCurrentSiteEditor);
			viewModel.Property = this.SiteManager.GetUserProfileProperty(id);

			return View("UserProfilePropertyEditor", viewModel);
		}
		
		[HttpPost]
		public ActionResult DeleteUserProfileProperty(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			this.SiteManager.DeleteUserProfileProperty(viewModel.Site, id);
			viewModel = BuildViewModel(this.SiteManager.Get(viewModel.Site.Id), viewModel.IsCurrentSiteEditor);

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult MovePropertyDown(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			this.SiteManager.MoveUserProfilePropertyDown(viewModel.Site, id);

			viewModel = BuildViewModel(viewModel.Site, true);

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult MovePropertyUp(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			this.SiteManager.MoveUserProfilePropertyUp(viewModel.Site, id);

			viewModel = BuildViewModel(viewModel.Site, true);

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult SaveUserProfileProperty(ViewModels.Admin.SiteEditor viewModel)
		{
			this.SiteManager.SaveUserProfileProperty(viewModel.Site, viewModel.Property);
			viewModel = BuildViewModel(this.SiteManager.Get(viewModel.Site.Id), viewModel.IsCurrentSiteEditor);

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult Select(ViewModels.Admin.SiteEditor viewModel)
		{
			return View("Editor", BuildViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult SelectAnotherIcon(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel.SelectedIconFile.ClearSelection();
			return View("Editor", BuildViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult SelectAnotherLogo(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel.SelectedLogoFile.ClearSelection();
			return View("Editor", BuildViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> UploadLogo(ViewModels.Admin.SiteEditor viewModel, [FromForm] IFormFile logoFile)
		{
			if (logoFile != null)
			{
				viewModel.SelectedLogoFile.Parent = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedLogoFile.Parent.Id);
				using (System.IO.Stream fileStream = logoFile.OpenReadStream())
				{
					viewModel.SelectedLogoFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedLogoFile.Provider, viewModel.SelectedLogoFile.Parent.Path, logoFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", BuildViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> UploadIcon(ViewModels.Admin.SiteEditor viewModel, [FromForm] IFormFile iconFile)
		{
			if (iconFile != null)
			{
				viewModel.SelectedIconFile.Parent = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedIconFile.Parent.Id);
				using (System.IO.Stream fileStream = iconFile.OpenReadStream())
				{
					viewModel.SelectedIconFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedIconFile.Provider, viewModel.SelectedIconFile.Parent.Path, iconFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", BuildViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult Save(ViewModels.Admin.SiteEditor viewModel)
		{
			// Not selecting a file is a valid choice, so we remove any validation errors for the logo/icon file controls
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.SelectedIconFile)}.{nameof(ViewModels.Admin.SiteEditor.SelectedIconFile.Id)}");
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.SelectedLogoFile)}.{nameof(ViewModels.Admin.SiteEditor.SelectedLogoFile.Id)}");

			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}
			
			if (viewModel.Site.Id == Guid.Empty)
			{
				viewModel.Site.Id = Guid.NewGuid();

				// new site.  Create administrators role and registered users role
				viewModel.Site.AdministratorsRole.Type = Role.RoleType.System;
				this.RoleManager.Save(viewModel.Site, viewModel.Site.AdministratorsRole);
				viewModel.Site.RegisteredUsersRole.Type = Role.RoleType.System;
				this.RoleManager.Save(viewModel.Site, viewModel.Site.RegisteredUsersRole);
				viewModel.Site.AnonymousUsersRole.Type = Role.RoleType.System;
				this.RoleManager.Save(viewModel.Site, viewModel.Site.AnonymousUsersRole);
				viewModel.Site.AllUsersRole.Type = Role.RoleType.System;
				this.RoleManager.Save(viewModel.Site, viewModel.Site.AllUsersRole);
			}

			viewModel.Site.SetSiteMailSettings(viewModel.MailSettings);
			viewModel.Site.SetSiteMailTemplates(viewModel.SiteTemplateSelections);
			viewModel.Site.SetSitePages(viewModel.SitePages);

			viewModel.Site.SiteSettings.TrySetValue(Site.SiteImageKeys.FAVICON_FILEID, viewModel.SelectedIconFile.Id);
			viewModel.Site.SiteSettings.TrySetValue(Site.SiteImageKeys.LOGO_FILEID, viewModel.SelectedLogoFile.Id);

			try
			{
				this.SiteManager.Save(viewModel.Site);
			}
			catch (ConstraintException e)
			{
				return BadRequest(e.ModelState);
			}

			if (viewModel.IsCurrentSiteEditor)
			{
				return Json(new { Title="Save Site", Message = "Changes Saved." });
			}
			else
			{
				return View("Index", BuildViewModel());
			}
		}

		private ViewModels.Admin.SiteIndex BuildViewModel()
		{
			ViewModels.Admin.SiteIndex viewModel = new ViewModels.Admin.SiteIndex();

			viewModel.Sites = this.SiteManager.List();				
			
			return viewModel;
		}

		private ViewModels.Admin.SiteEditor BuildViewModel(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel.Site.Aliases = this.SiteManager.ListAliases(viewModel.Site);
			viewModel.IsCurrentSite = (viewModel.Site.Id == this.Context.Site.Id);

			viewModel.MailTemplates = this.MailTemplateManager.List(viewModel.Site);
			viewModel.Pages = this.PageManager.List(viewModel.Site);
			viewModel.Roles = this.RoleManager.List(viewModel.Site);
			viewModel.Layouts = this.LayoutManager.List();
			// todo
			viewModel.SiteGroups = new List<SiteGroup>();

			return viewModel;
		}

		private ViewModels.Admin.SiteEditor BuildViewModel(Site site, Boolean isCurrentSiteEditor)
		{
			ViewModels.Admin.SiteEditor viewModel = new ViewModels.Admin.SiteEditor();

			viewModel.Site = site;
			viewModel.IsCurrentSiteEditor = isCurrentSiteEditor;

			//viewModel.Roles = this.RoleManager.List(site);

			viewModel.MailSettings = site.GetSiteMailSettings();

			viewModel.MailSettings.Password = SiteExtensions.UNCHANGED_PASSWORD;
			viewModel.SiteTemplateSelections = viewModel.Site.GetSiteTemplateSelections();
			viewModel.SitePages = viewModel.Site.GetSitePages();


			//viewModel.Pages = this.PageManager.List(site);
			
			//viewModel.Layouts = this.LayoutManager.List();

			viewModel.SelectedIconFile = new();
			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SiteImageKeys.FAVICON_FILEID), out Guid iconFileId))
			{
				viewModel.SelectedIconFile.Id = iconFileId;
			}

			viewModel.SelectedLogoFile = new();
			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SiteImageKeys.LOGO_FILEID), out Guid logoFileId))
			{
				viewModel.SelectedLogoFile.Id = logoFileId;
			}

			return BuildViewModel(viewModel);
		}
	}
}
