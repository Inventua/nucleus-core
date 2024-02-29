using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Http;
using Nucleus.Data.Common;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]	
	public class SitesController : Controller
	{
		private ILogger<SitesController> Logger { get; }
		private ISiteManager SiteManager{ get; }
		private IRoleManager RoleManager { get; }
		private IPageManager PageManager { get; }
		private IMailTemplateManager MailTemplateManager { get; }
		private IMailClientFactory MailClientFactory { get; }

		private IFileSystemManager FileSystemManager { get; }		
		private Context Context { get; }
		private ILayoutManager LayoutManager { get; }
		private IContainerManager ContainerManager { get; }

		public SitesController(Context context, ILogger<SitesController> logger, ISiteManager siteManager, IPageManager pageManager, IMailTemplateManager mailTemplateManager, IMailClientFactory mailClientFactory, IRoleManager roleManager, ILayoutManager layoutManager, IContainerManager containerManager, IFileSystemManager fileSystemManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.SiteManager = siteManager;
			this.PageManager = pageManager;
			this.MailTemplateManager = mailTemplateManager;
			this.MailClientFactory = mailClientFactory;
			this.RoleManager = roleManager;
			this.LayoutManager = layoutManager;
			this.ContainerManager = containerManager;
			this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		/// Display the site editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// Display the Site editor for the "current" site
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> EditCurrentSite()
		{
			ViewModels.Admin.SiteEditor viewModel;
						
			viewModel = await BuildViewModel(this.Context.Site, true);
			
			return View("Editor", viewModel);
		}

		/// <summary>
		/// Display the Site editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.SiteEditor viewModel;
						
			viewModel = await BuildViewModel(id == Guid.Empty ? await this.SiteManager.CreateNew() : await this.SiteManager.Get(id), false);
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
		public async Task<ActionResult> AddSite()
		{
			return View("Editor", await BuildViewModel(new Site(), false));
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

		[HttpGet]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
		public async Task<ActionResult> ExportSite(Guid siteId)
		{
			Site site = await this.SiteManager.Get(siteId);
			System.IO.Stream export = await this.SiteManager.Export(site);
			return File(export, "text/xml", $"site-{site.Name}.xml");
		}


		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
		public async Task<ActionResult> DeleteSite(ViewModels.Admin.SiteEditor viewModel)
		{
			await this.SiteManager.Delete(viewModel.Site);
			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> AddAlias(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel = await BuildViewModel(viewModel);
			viewModel.Alias = new();
			
			return View("AliasEditor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> EditAlias(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			viewModel = await BuildViewModel(viewModel);
			viewModel.Alias = await this.SiteManager.GetAlias(id);
			
			return View("AliasEditor", viewModel);
		}


		[HttpPost]
		public async Task<ActionResult> SaveAlias(ViewModels.Admin.SiteEditor viewModel)
		{			
			await this.SiteManager.SaveAlias(viewModel.Site, viewModel.Alias);
			viewModel = await BuildAliasViewModel(viewModel);

			return View("_AliasList", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> DeleteAlias(Guid id, ViewModels.Admin.SiteEditor viewModel)
		{			
			await this.SiteManager.DeleteAlias(viewModel.Site, id);
			viewModel = await BuildAliasViewModel(viewModel);

			return View("_AliasList", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddUserProfileProperty(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel = await BuildViewModel(viewModel);
			viewModel.Property = new();

			return View("UserProfilePropertyEditor", viewModel);
		}	
			
		[HttpPost]
		public async Task<ActionResult> EditUserProfileProperty(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			viewModel = await BuildViewModel(viewModel);
			viewModel.Property = await this.SiteManager.GetUserProfileProperty(id);

			return View("UserProfilePropertyEditor", viewModel);
		}
		
		[HttpPost]
		public async Task<ActionResult> DeleteUserProfileProperty(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			await this.SiteManager.DeleteUserProfileProperty(viewModel.Site, id);
			viewModel.Site.UserProfileProperties = (await this.SiteManager.Get(viewModel.Site.Id)).UserProfileProperties;

			return View("_UserProfilePropertiesList", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> MovePropertyDown(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			await this.SiteManager.MoveUserProfilePropertyDown(viewModel.Site, id);

			viewModel.Site.UserProfileProperties = (await this.SiteManager.Get(viewModel.Site.Id)).UserProfileProperties;

			return View("_UserProfilePropertiesList", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> MovePropertyUp(ViewModels.Admin.SiteEditor viewModel, Guid id)
		{
			await this.SiteManager.MoveUserProfilePropertyUp(viewModel.Site, id);
			viewModel.Site.UserProfileProperties = (await this.SiteManager.Get(viewModel.Site.Id)).UserProfileProperties;
 			return View("_UserProfilePropertiesList", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> SaveUserProfileProperty(ViewModels.Admin.SiteEditor viewModel)
		{
			await this.SiteManager.SaveUserProfileProperty(viewModel.Site, viewModel.Property);
			viewModel.Site.UserProfileProperties = (await this.SiteManager.Get(viewModel.Site.Id)).UserProfileProperties;

			return View("_UserProfilePropertiesList", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> Select(ViewModels.Admin.SiteEditor viewModel)
		{
			return View("Editor", await BuildViewModel(viewModel));
		}

		
		[HttpPost]
		public async Task<ActionResult> SelectAnotherCssFile(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel.SelectedCssFile.ClearSelection();
			return View("Editor", await BuildViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> SelectAnotherIcon(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel.SelectedIconFile.ClearSelection();
			return View("Editor", await BuildViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> SelectAnotherLogo(ViewModels.Admin.SiteEditor viewModel)
		{
			viewModel.SelectedLogoFile.ClearSelection();
			return View("Editor", await BuildViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> UploadLogo(ViewModels.Admin.SiteEditor viewModel, [FromForm] IFormFile logoFile)
		{
			if (logoFile != null)
			{
				viewModel.SelectedLogoFile.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedLogoFile.Parent.Id);
				using (System.IO.Stream fileStream = logoFile.OpenReadStream())
				{
					viewModel.SelectedLogoFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedLogoFile.Provider, viewModel.SelectedLogoFile.Parent.Path, logoFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", await BuildViewModel (viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> UploadCssFile(ViewModels.Admin.SiteEditor viewModel, [FromForm] IFormFile cssFile)
		{
			if (cssFile != null)
			{
				viewModel.SelectedIconFile.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedCssFile.Parent.Id);
				using (System.IO.Stream fileStream = cssFile.OpenReadStream())
				{
					viewModel.SelectedCssFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedIconFile.Provider, viewModel.SelectedCssFile.Parent.Path, cssFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", await BuildViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> UploadIcon(ViewModels.Admin.SiteEditor viewModel, [FromForm] IFormFile iconFile)
		{
			if (iconFile != null)
			{
				viewModel.SelectedIconFile.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedIconFile.Parent.Id);
				using (System.IO.Stream fileStream = iconFile.OpenReadStream())
				{
					viewModel.SelectedIconFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedIconFile.Provider, viewModel.SelectedIconFile.Parent.Path, iconFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Editor", await BuildViewModel (viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.SiteEditor viewModel)
		{
			Boolean isNew = false;

			// Not selecting a file is a valid choice, so we remove any validation errors for the logo/icon file controls
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.SelectedCssFile)}.{nameof(ViewModels.Admin.SiteEditor.SelectedCssFile.Id)}");
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.SelectedIconFile)}.{nameof(ViewModels.Admin.SiteEditor.SelectedIconFile.Id)}");
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.SelectedLogoFile)}.{nameof(ViewModels.Admin.SiteEditor.SelectedLogoFile.Id)}");
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.Site)}.{nameof(ViewModels.Admin.SiteEditor.Site.DefaultSiteAlias)}.{nameof(ViewModels.Admin.SiteEditor.Site.DefaultSiteAlias.Alias)}");

			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.Site)}.{nameof(ViewModels.Admin.SiteEditor.Site.AdministratorsRole)}.{nameof(ViewModels.Admin.SiteEditor.Site.AdministratorsRole.Name)}");
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.Site)}.{nameof(ViewModels.Admin.SiteEditor.Site.AllUsersRole)}.{nameof(ViewModels.Admin.SiteEditor.Site.AllUsersRole.Name)}");
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.Site)}.{nameof(ViewModels.Admin.SiteEditor.Site.AnonymousUsersRole)}.{nameof(ViewModels.Admin.SiteEditor.Site.AnonymousUsersRole.Name)}");
			ControllerContext.ModelState.Remove($"{nameof(ViewModels.Admin.SiteEditor.Site)}.{nameof(ViewModels.Admin.SiteEditor.Site.RegisteredUsersRole)}.{nameof(ViewModels.Admin.SiteEditor.Site.RegisteredUsersRole.Name)}");

      // Validate the site home directory.  
      Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = viewModel.Site.ValidateHomeDirectory($"{nameof(viewModel.Site)}.{nameof(viewModel.Site.HomeDirectory)}");
      if (!modelState.IsValid)
      {
        return BadRequest(modelState);
      }

      if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}
			
			if (viewModel.Site.Id == Guid.Empty)
			{
				isNew = true;
			}

			if (viewModel.AllowPublicRegistration)
			{
				viewModel.Site.UserRegistrationOptions = viewModel.Site.UserRegistrationOptions |= Site.SiteUserRegistrationOptions.SignupAllowed;
			}
			else
			{
				viewModel.Site.UserRegistrationOptions = Site.SiteUserRegistrationOptions.NoSignup;
			}

			if (viewModel.RequireApproval)
			{
				viewModel.Site.UserRegistrationOptions = viewModel.Site.UserRegistrationOptions |= Site.SiteUserRegistrationOptions.RequireApproval;
			}
			else
			{
				viewModel.Site.UserRegistrationOptions &= ~Site.SiteUserRegistrationOptions.RequireApproval;
			}

			if (viewModel.RequireEmailVerification)
			{
				viewModel.Site.UserRegistrationOptions = viewModel.Site.UserRegistrationOptions |= Site.SiteUserRegistrationOptions.RequireEmailVerification;
			}
			else
			{
				viewModel.Site.UserRegistrationOptions &= ~Site.SiteUserRegistrationOptions.RequireEmailVerification;
			}

			viewModel.Site.SetSiteMailSettings(viewModel.MailSettings);
			viewModel.Site.SetSiteMailTemplates(viewModel.SiteTemplateSelections);
			viewModel.Site.SetSitePages(viewModel.SitePages);

			viewModel.Site.SiteSettings.TrySetValue(Site.SiteFilesKeys.CSSFILE_FILEID, viewModel.SelectedCssFile?.Id);
			viewModel.Site.SiteSettings.TrySetValue(Site.SiteFilesKeys.FAVICON_FILEID, viewModel.SelectedIconFile?.Id);
			viewModel.Site.SiteSettings.TrySetValue(Site.SiteFilesKeys.LOGO_FILEID, viewModel.SelectedLogoFile?.Id);

			await this.SiteManager.Save(viewModel.Site);

			if (isNew)
			{
				// new sites are with empty roles, because the roles can't be created until the site record exists.  Create roles, update site.
				viewModel.Site.AdministratorsRole.Type = Role.RoleType.System;
				await this.RoleManager.Save(viewModel.Site, viewModel.Site.AdministratorsRole);
				viewModel.Site.RegisteredUsersRole.Type = Role.RoleType.System;
				await this.RoleManager.Save(viewModel.Site, viewModel.Site.RegisteredUsersRole);
				viewModel.Site.AnonymousUsersRole.Type = Role.RoleType.System | Role.RoleType.Restricted;
				await this.RoleManager.Save(viewModel.Site, viewModel.Site.AnonymousUsersRole);
				viewModel.Site.AllUsersRole.Type = Role.RoleType.System | Role.RoleType.Restricted;
				await this.RoleManager.Save(viewModel.Site, viewModel.Site.AllUsersRole);

				await this.SiteManager.Save(viewModel.Site);
			}

			if (viewModel.IsCurrentSiteEditor)
			{
				return Ok();
			}
			else
			{
				return View("Index", await BuildViewModel());
			}
		}

		[HttpPost]
		public async Task<ActionResult> TestMailSettings(ViewModels.Admin.SiteEditor viewModel)
		{
			// we must retrieve the site from the database because the user may be using the "test" button just after loading
			// data - and in that case the password will be set to a dummy value.
			Site site = await this.SiteManager.Get(viewModel.Site.Id);
			site.SetSiteMailSettings(viewModel.MailSettings);

			using (IMailClient client = this.MailClientFactory.Create(site))
			{
				try
				{
					await client.Send(new Abstractions.Models.Mail.MailTemplate() { Subject = $"Email Configuration Test from {viewModel.Site.Name}, {DateTime.Now}", Body =  $"This email was generated as a test by the user {User.Identity.Name} at {DateTime.Now}.  If you received it, then your site's email configuration is working correctly." }, new object(), viewModel.MailSettings.Sender);
				}
				catch (Exception ex)
				{
					return BadRequest(ex.Message);
				}
			}

			return Json(new { Title = "Test Email Settings", Message = $"A test email was sent successfully to '{viewModel.MailSettings.Sender}'.", Icon = "alert" });
		}


			private async Task<ViewModels.Admin.SiteIndex> BuildViewModel()
		{
			ViewModels.Admin.SiteIndex viewModel = new();

			viewModel.Sites = await this.SiteManager.List();				
			
			return viewModel;
		}

		private async Task<ViewModels.Admin.SiteEditor> BuildAliasViewModel(ViewModels.Admin.SiteEditor viewModel)
		{
			Site siteData = await this.SiteManager.Get(viewModel.Site.Id);
			viewModel.Site.Aliases = siteData.Aliases;
			viewModel.Site.DefaultSiteAlias = siteData.DefaultSiteAlias;

			return viewModel;
		}
				
		private async Task<ViewModels.Admin.SiteEditor> BuildViewModel(ViewModels.Admin.SiteEditor viewModel)
		{
			// Aliases are not held in hidden fields & are lost between postbacks
			Site fullSite = await this.SiteManager.Get(viewModel.Site.Id);

			if (fullSite != null)
			{
				viewModel.Site.Aliases = fullSite?.Aliases;
				viewModel.Site.UserProfileProperties = fullSite?.UserProfileProperties;

				viewModel.AllowPublicRegistration = fullSite.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.SignupAllowed);
				viewModel.RequireApproval = fullSite.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.RequireApproval);
				viewModel.RequireEmailVerification = fullSite.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.RequireEmailVerification);
			}

			viewModel.IsCurrentSite = (viewModel.Site.Id == this.Context.Site.Id);

			viewModel.MailTemplates = await this.MailTemplateManager.List(viewModel.Site);
			viewModel.Pages = await this.PageManager.List(viewModel.Site);
			viewModel.PageMenu = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, 1);
			viewModel.Roles = await this.RoleManager.List(viewModel.Site);
			viewModel.Layouts = (await this.LayoutManager.List()).InsertDefaultListItem();
			viewModel.Containers = (await this.ContainerManager.List()).InsertDefaultListItem();

			// In version 1, there is no implementation for site groups, and an empty site groups list prevents the site groups drop-down from appearing.
			viewModel.SiteGroups = new List<SiteGroup>();

			viewModel.SelectedCssFile = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.SelectedCssFile);
			viewModel.SelectedIconFile = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.SelectedIconFile);
			viewModel.SelectedLogoFile = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.SelectedLogoFile);

			return viewModel;
		}

		private async Task<ViewModels.Admin.SiteEditor> BuildViewModel(Site site, Boolean isCurrentSiteEditor)
		{
			ViewModels.Admin.SiteEditor viewModel = new();

			viewModel.Site = site;
			viewModel.IsCurrentSiteEditor = isCurrentSiteEditor;

			viewModel.AllowPublicRegistration = site.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.SignupAllowed);
			viewModel.RequireApproval = site.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.RequireApproval);
			viewModel.RequireEmailVerification= site.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.RequireEmailVerification);

			viewModel.MailSettings = site.GetSiteMailSettings();
			viewModel.MailSettings.Password = SiteExtensions.UNCHANGED_PASSWORD;

			viewModel.SiteTemplateSelections = viewModel.Site.GetSiteTemplateSelections();
			viewModel.SitePages = viewModel.Site.GetSitePages();

			if (viewModel.SelectedCssFile == null)
			{
				viewModel.SelectedCssFile = new();
				if (site.SiteSettings.TryGetValue(Site.SiteFilesKeys.CSSFILE_FILEID, out Guid cssFileId))
				{
					viewModel.SelectedCssFile.Id = cssFileId;
				}
			}

			if (viewModel.SelectedIconFile == null)
			{
				viewModel.SelectedIconFile = new();
				if (site.SiteSettings.TryGetValue(Site.SiteFilesKeys.FAVICON_FILEID, out Guid iconFileId))
				{
					viewModel.SelectedIconFile.Id = iconFileId;
				}
			}

			if (viewModel.SelectedLogoFile == null)
			{
				viewModel.SelectedLogoFile = new();
				if (site.SiteSettings.TryGetValue(Site.SiteFilesKeys.LOGO_FILEID, out Guid logoFileId))
				{
					viewModel.SelectedLogoFile.Id = logoFileId;
				}
			}

			return await BuildViewModel(viewModel);
		}
	}
}
