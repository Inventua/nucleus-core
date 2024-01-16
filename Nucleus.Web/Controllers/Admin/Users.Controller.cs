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
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions;
using Nucleus.Extensions.Excel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Models.Mail;
using System.Security.Claims;
using Nucleus.ViewFeatures;

namespace Nucleus.Web.Controllers.Admin
{
    [Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class UsersController : Controller
	{
		private ILogger<UsersController> Logger { get; }
		private IUserManager UserManager { get; }
		private IRoleManager RoleManager { get; }
		private ClaimTypeOptions ClaimTypeOptions { get; }
    private PasswordOptions PasswordOptions { get; }
    private IMailTemplateManager MailTemplateManager { get; }
    private IMailClientFactory MailClientFactory { get; }
    private IPageManager PageManager { get; }

    private Context Context { get; }

		public UsersController(Context context, ILogger<UsersController> logger, IUserManager userManager, IRoleManager roleManager, IPageManager pageManager, IMailTemplateManager mailTemplateManager, IMailClientFactory mailClientFactory, IOptions< ClaimTypeOptions> claimTypeOptions, IOptions<PasswordOptions> passwordOptions)
		{
			this.Context = context;
			this.Logger = logger;
			this.RoleManager = roleManager;
			this.UserManager = userManager;
      this.PageManager = pageManager;
      this.MailTemplateManager = mailTemplateManager;
      this.MailClientFactory = mailClientFactory;
			this.ClaimTypeOptions = claimTypeOptions.Value;
      this.PasswordOptions = passwordOptions.Value;
		}

		/// <summary>
		/// Display the user list
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

    /// <summary>
    /// Display the user list
    /// </summary>
    /// <returns></returns>
    [HttpPost]
		public async Task<ActionResult> List(ViewModels.Admin.UserIndex viewModel)
		{
			return View("_UserList", await BuildViewModel(viewModel));
		}

		///// <summary>
		///// Search for users containing the specified search term.
		///// </summary>
		///// <returns></returns>
		//[HttpPost]
		//public async Task<ActionResult> Search(ViewModels.Admin.UserIndex viewModel)
		//{
		//	viewModel.SearchResults = await this.UserManager.Search(this.Context.Site, viewModel.SearchTerm, viewModel.SearchResults, BuildFilter(viewModel.FilterSelections));
		//	viewModel.Site = this.Context.Site;

		//	return View("SearchResults", viewModel);
		//}

		/// <summary>
		/// Export all users to excel.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Export()
		{
			IList<User> users = await this.UserManager.List(this.Context.Site);
      
			var exporter = new ExcelWriter<User>
			(
        ExcelWorksheet.Modes.IncludeSpecifiedPropertiesOnly
			);

			exporter.AddColumn(user => user.Id);
			exporter.AddColumn(user => user.UserName);
			exporter.AddColumn(user => user.Approved);
			exporter.AddColumn(user => user.Verified);
			exporter.AddColumn(user => user.Roles);

			foreach (UserProfileProperty profileProperty in this.Context.Site.UserProfileProperties)
			{
				exporter.AddColumn(profileProperty.Name, profileProperty.Name, ClosedXML.Excel.XLDataType.Text, 
					user => user.Profile
						.Where(profile => profile.UserProfileProperty.Id == profileProperty.Id)
						.Select(profileValue => profileValue.Value)
						.FirstOrDefault());
			}

			exporter.AddColumn(user => user.DateAdded);
			exporter.AddColumn(user => user.DateChanged);

			exporter.Worksheet.Name = "Users";
			exporter.Export(users);

			return File(exporter.GetOutputStream(), ExcelWriter.MIMETYPE_EXCEL, $"Users Export {DateTime.Now}.xlsx");			
		}		

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.UserEditor viewModel;

			viewModel = await BuildViewModel(id == Guid.Empty ? await this.UserManager.CreateNew(this.Context.Site) : await this.UserManager.Get(this.Context.Site, id));

			return View("Editor", viewModel);
		}

		[HttpGet]
		public async Task<ActionResult> AddUser()
		{
			User newUser = await this.UserManager.CreateNew(this.Context.Site);
			newUser.Verified = true;
			newUser.Approved = true;

			return View("Editor", await BuildViewModel( newUser ));
		}

		[HttpPost]
		public async Task<ActionResult> AddRole(ViewModels.Admin.UserEditor viewModel)
		{
			await this.UserManager.AddRole(viewModel.User, viewModel.SelectedRoleId);
			//this.UserManager.SetupUserProfileProperties(viewModel.User);
			return View("Editor", await BuildViewModel(viewModel.User));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.UserEditor viewModel)
		{			
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			// only save a password for a new user (and if they entered one)
			if (viewModel.User.Id == Guid.Empty)
			{
				if (viewModel.User.Secrets == null)
				{
					viewModel.User.Secrets = new();
				}

				if (!String.IsNullOrEmpty(viewModel.EnteredPassword))
        {
					Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.EnteredPassword), viewModel.EnteredPassword);
					if (!modelState.IsValid)
					{
						return BadRequest(modelState);
					}
								
					viewModel.User.Secrets.SetPassword(viewModel.EnteredPassword);
          if (this.PasswordOptions.PasswordExpiry.HasValue)
          {
            viewModel.User.Secrets.PasswordExpiryDate = DateTime.UtcNow.Add(this.PasswordOptions.PasswordExpiry.Value);
          }
        }
        else
        {
					return BadRequest("Please enter a password.");
        }
			}
			else
      {
				// We must re-read viewModel.User.Secrets for existing users because model binding always creates a new (empty) .Secrets object, which would cause
				// the data provider to overwrite an existing user's secrets record with blanks.  

				viewModel.User.Secrets = (await this.UserManager.Get(viewModel.User.Id)).Secrets;
			}

			// If the user has no roles assigned (or they have all been deleted), model binding will set .Roles to null - which is interpreted
			// in the data provider as meaning "don't save role assignments".  We need to set roles to an empty list so that role assignments are
			// saved (that is, existing roles are removed).
			if (viewModel.User.Roles == null)
			{
				viewModel.User.Roles = new();
			}

			// If the user being edited is the currently logged in user, the approved and verified fields won't be shown, so we need
			// to get them  from the existing record
			if (viewModel.User.Id == ControllerContext.HttpContext.User.GetUserId())
			{
				User existing = await this.UserManager.Get(this.Context.Site, viewModel.User.Id);
				if (existing != null)
				{
					viewModel.User.Verified = existing.Verified;
					viewModel.User.Approved = existing.Approved;
				}
			}

      if (viewModel.User.Secrets != null && viewModel.ExpirePassword)
      {
        viewModel.User.Secrets.PasswordExpiryDate = DateTime.UtcNow;
      }

			await this.UserManager.Save(this.Context.Site, viewModel.User);

			return View("Index", await BuildViewModel());
		}

    [HttpPost]
    public async Task<ActionResult> UnlockUser(ViewModels.Admin.UserEditor viewModel)
    {
      if (!ControllerContext.ModelState.IsValid)
      {
        return BadRequest(ControllerContext.ModelState);
      }

      // only save a password for a new user (and if they entered one)
      if (viewModel.User.Id == Guid.Empty)
      {
        return BadRequest();        
      }
      else
      {
        User user = await this.UserManager.Get(this.Context.Site, viewModel.User.Id);
        if (user == null)
        {
          return BadRequest();
        }
        else
        {
          // We must re-read viewModel.User.Secrets for existing users because model binding always creates a new (empty) .Secrets object, which would cause
          // the data provider to overwrite an existing user's secrets record with blanks.  
          await this.UserManager.UnlockUser(user);
        }
        return View("Editor", await BuildViewModel(user));
      }
    }

    [HttpPost]
    public async Task<ActionResult> SendVerification(ViewModels.Admin.UserEditor viewModel)
    {
      if (!ControllerContext.ModelState.IsValid)
      {
        return BadRequest(ControllerContext.ModelState);
      }

      // only save a password for a new user (and if they entered one)
      if (viewModel.User.Id == Guid.Empty)
      {
        return BadRequest();
      }
      else
      {
        User user = await this.UserManager.Get(this.Context.Site, viewModel.User.Id);
        if (user == null)
        {
          return BadRequest();
        }
        else
        {
          UserProfileValue email = user.Profile.GetProperty(ClaimTypes.Email);

          if (!String.IsNullOrEmpty(email?.Value))
          {
            SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

            if (templateSelections.WelcomeNewUserTemplateId.HasValue)
            {
              MailTemplate template = await this.MailTemplateManager.Get(templateSelections.WelcomeNewUserTemplateId.Value);
              if (template != null)
              {
                await this.UserManager.SetVerificationToken(user);

                SitePages sitePages = this.Context.Site.GetSitePages();

                Abstractions.Models.Mail.Template.UserMailTemplateData args = new()
                {
                  Site = this.Context.Site,
                  User = user.GetCensored(),
                  Url = new System.Uri(await GetLoginPageUri(), $"?token={user.Secrets.VerificationToken}").ToString(),
                  LoginPage = sitePages.LoginPageId.HasValue ? await this.PageManager.Get(sitePages.LoginPageId.Value) : null,
                  PrivacyPage = sitePages.PrivacyPageId.HasValue ? await this.PageManager.Get(sitePages.PrivacyPageId.Value) : null,
                  TermsPage = sitePages.TermsPageId.HasValue ? await this.PageManager.Get(sitePages.TermsPageId.Value) : null
                };

                Logger.LogTrace("Sending password reset email {templateName} to user {userId}.", template.Name, user.Id);

                using (IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
                {
                  await mailClient.Send(template, args, email.Value);
                  return Json(new { Title = "Password Reset", Message = "Password Reset email sent.", Icon = "alert" });
                }
              }
            }
            else
            {
              Logger.LogTrace("Not sending password reset to user {userId} because no password reset template is configured for site {siteId}.", user.Id, this.Context.Site.Id);
              return Json(new { Title = "Password Reset", Message = "Your site administrator has not configured a password reset email template.  Please contact the site administrator for help.", Icon = "error" });
            }
          }

          else
          {
            return BadRequest();
          }
        }
      }

      return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SendPasswordReset(ViewModels.Admin.UserEditor viewModel)
    {
      if (!ControllerContext.ModelState.IsValid)
      {
        return BadRequest(ControllerContext.ModelState);
      }

      // only save a password for a new user (and if they entered one)
      if (viewModel.User.Id == Guid.Empty)
      {
        return BadRequest();
      }
      else
      {
        User user = await this.UserManager.Get(this.Context.Site, viewModel.User.Id);
        if (user == null)
        {
          return BadRequest();
        }
        else
        {
          UserProfileValue email = user.Profile.GetProperty(ClaimTypes.Email);

          if (!String.IsNullOrEmpty(email?.Value))
          {
            SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

            if (templateSelections.PasswordResetTemplateId.HasValue)
            {
              MailTemplate template = await this.MailTemplateManager.Get(templateSelections.PasswordResetTemplateId.Value);
              if (template != null)
              {
                await this.UserManager.SetPasswordResetToken(user);

                //Nucleus.Abstractions.Models.Mail.RecoveryEmailModel args = new()
                //{
                //  Site = this.Context.Site,
                //  User = viewModel.User.GetCensored(),
                //  Url = new System.Uri(await GetLoginPageUri(), $"?token={viewModel.User.Secrets.PasswordResetToken}").ToString()
                //};
                SitePages sitePages = this.Context.Site.GetSitePages();

                Abstractions.Models.Mail.Template.UserMailTemplateData args = new()
                {
                  Site = this.Context.Site,
                  User = user.GetCensored(),
                  Url = new System.Uri(await GetLoginPageUri(), $"?token={user.Secrets.PasswordResetToken}").ToString(),
                  LoginPage = sitePages.LoginPageId.HasValue ? await this.PageManager.Get(sitePages.LoginPageId.Value) : null,
                  PrivacyPage = sitePages.PrivacyPageId.HasValue ? await this.PageManager.Get(sitePages.PrivacyPageId.Value) : null,
                  TermsPage = sitePages.TermsPageId.HasValue ? await this.PageManager.Get(sitePages.TermsPageId.Value) : null
                };

                Logger.LogTrace("Sending password reset email {templateName} to user {userId}.", template.Name, user.Id);

                using (IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
                {
                  await mailClient.Send(template, args, email.Value);
                  return Json(new { Title = "Password Reset", Message = "Password Reset email sent.", Icon = "alert" });
                }
              }
            }
            else
            {
              Logger.LogTrace("Not sending password reset to user {userId} because no password reset template is configured for site {siteId}.", user.Id, this.Context.Site.Id);
              return Json(new { Title = "Password Reset", Message = "Your site administrator has not configured a password reset email template.  Please contact the site administrator for help.", Icon = "error" });
            }
          }

          else
          {
            return BadRequest();
          }
        }
      }

      return Ok();
    }

    [HttpPost]
		public async Task<ActionResult> RemoveUserRole(ViewModels.Admin.UserEditor viewModel, Guid roleId)
		{
			await this.UserManager.RemoveRole(viewModel.User, roleId);
			this.ControllerContext.ModelState.RemovePrefix("User.Roles");
			return View("Editor", await BuildViewModel(viewModel.User));
		}

		[HttpPost]
		public async Task<ActionResult> DeleteUser(ViewModels.Admin.UserEditor viewModel)
		{
			await this.UserManager.Delete(viewModel.User);
			return View("Index", await BuildViewModel());
		}

    private async Task<IEnumerable<SelectListItem>> GetAvailableUserRoles(User user)
    {
      IEnumerable<Role> availableRoles = (await this.RoleManager.List(this.Context.Site))
        .Where
        (
          role => !role.Type.HasFlag(Role.RoleType.Restricted) && !user.Roles?.Contains(role) == true
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


    private async Task<IEnumerable<SelectListItem>> GetFilterRoles()
    {
      IEnumerable<Role> availableRoles = (await this.RoleManager.List(this.Context.Site))
        .Where
        (
          role => !role.Type.HasFlag(Role.RoleType.Restricted)
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
        
    private System.Linq.Expressions.Expression<Func<User, bool>> BuildFilter(ViewModels.Admin.UserFilterOptions filterOptions)
    {
      return 
      (
        user =>        
        (
          filterOptions.RoleId == null || (user.Roles.Where(role => role.Id == filterOptions.RoleId).Any())
        ) 
        &&
        (
          filterOptions.Approved == ViewModels.Admin.UserFilterOptions.ApprovedFilter.All ||
          (filterOptions.Approved == ViewModels.Admin.UserFilterOptions.ApprovedFilter.ApprovedOnly && user.Approved) ||
          (filterOptions.Approved == ViewModels.Admin.UserFilterOptions.ApprovedFilter.NotApprovedOnly && !user.Approved)
        )
        &&
        (
          filterOptions.Verified == ViewModels.Admin.UserFilterOptions.VerifiedFilter.All ||
          (filterOptions.Verified == ViewModels.Admin.UserFilterOptions.VerifiedFilter.VerifiedOnly && user.Verified) ||
          (filterOptions.Verified == ViewModels.Admin.UserFilterOptions.VerifiedFilter.NotVerifiedOnly && !user.Verified)
        )
      );
    }

		private async Task<ViewModels.Admin.UserIndex> BuildViewModel()
		{
			return await BuildViewModel(new ViewModels.Admin.UserIndex());
		}

		private async Task<ViewModels.Admin.UserIndex> BuildViewModel(ViewModels.Admin.UserIndex viewModel)
		{
      if (String.IsNullOrEmpty(viewModel.SearchTerm))
      {
        viewModel.Users = await this.UserManager.List(this.Context.Site, viewModel.Users, BuildFilter(viewModel.FilterSelections));
      }
      else
      {
        viewModel.Users = await this.UserManager.Search(this.Context.Site, viewModel.SearchTerm, viewModel.Users, BuildFilter(viewModel.FilterSelections));
      }

			viewModel.Site = this.Context.Site;
      viewModel.FilterRoles = await GetFilterRoles();
      return viewModel;
		}

    private async Task<ViewModels.Admin.UserEditor> BuildViewModel(User user)
		{
			ViewModels.Admin.UserEditor viewModel = new();

			viewModel.User = user;
			viewModel.ClaimTypeOptions = this.ClaimTypeOptions;
			
			viewModel.IsCurrentUser = (user.Id == ControllerContext.HttpContext.User.GetUserId());
      viewModel.IsPasswordExpired = user.Secrets?.PasswordExpiryDate < DateTime.UtcNow;

      if (viewModel.User != null)
			{
        viewModel.AvailableRoles = await GetAvailableUserRoles(viewModel.User);
        
        // we must re-read secrets because this function is called with a user object that has been deserialized by MVC and will not 
        // contain secrets
        viewModel.User.Secrets = (await this.UserManager.Get(viewModel.User.Id))?.Secrets;
        if (viewModel.User.Secrets != null && viewModel.User.Secrets.IsLockedOut && viewModel.User.Secrets.LastLockoutDate.HasValue)
        {
          viewModel.LockoutResetDate = viewModel.User.Secrets.LastLockoutDate.Value.Add(this.PasswordOptions.FailedPasswordLockoutReset);
        }
      }

			return viewModel;
		}

    private async Task<System.Uri> GetLoginPageUri()
    {
      PageRoute loginPageRoute;
      Page loginPage = await this.PageManager.Get(this.Context.Site.GetSitePages().LoginPageId.Value);
      if (loginPage != null)
      {
        loginPageRoute = loginPage.DefaultPageRoute();
        return Url.GetAbsoluteUri(loginPageRoute.Path);
      }
      else
      {
        //RouteValueDictionary routeDictionary = new();

        //routeDictionary.Add("area", "User");
        //routeDictionary.Add("controller", "Account");
        //routeDictionary.Add("action", "Index");
        //return Url.GetAbsoluteUri(this.LinkGenerator.GetPathByRouteValues("Admin", routeDictionary, this.ControllerContext.HttpContext.Request.PathBase, FragmentString.Empty, null));

        return Url.GetAbsoluteUri(this.Url.AreaAction("Index", "Account", "User"));
        
      }
    }
  }
}
