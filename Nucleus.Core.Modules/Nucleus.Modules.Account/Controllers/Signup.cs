using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.ViewFeatures;

namespace Nucleus.Modules.Account.Controllers
{
	[Extension("Account")]
	public class SignupController : Controller
	{
		private Context Context { get; }
		private ILogger<LoginController> Logger { get; }
		private IUserManager UserManager { get; }
		private IPageManager PageManager { get; }

		private ClaimTypeOptions ClaimTypeOptions { get; }

		public SignupController(Context context, ILogger<LoginController> Logger, IUserManager userManager, IPageManager pageManager, IOptions<ClaimTypeOptions> claimTypeOptions)
		{
			this.Context = context;
			this.Logger = Logger;
			this.UserManager = userManager;
			this.PageManager = pageManager;
			this.ClaimTypeOptions = claimTypeOptions.Value;
		}

		[HttpGet]
		public async Task<ActionResult> Index(string returnUrl)
		{
			return View("Signup", await BuildViewModel(returnUrl));
		}

		[HttpGet]
		public ActionResult Edit(string returnUrl)
		{
			return View("SignupSettings", new ViewModels.Signup());
		}

		[HttpPost]
		public async Task<ActionResult> Signup(ViewModels.Signup viewModel)
		{
			if (!this.ModelState.IsValid)
			{
				return BadRequest(this.ModelState);
			}

			if (viewModel.ConfirmPassword != viewModel.NewPassword)
			{
				return BadRequest("Your new password and confirm password values do not match");
			}
			
			Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.NewPassword), viewModel.NewPassword);
			if (!modelState.IsValid)
			{
				return BadRequest(modelState);
			}
			
			try
			{
				User newUser = await this.UserManager.CreateNew(this.Context.Site);

				newUser.UserName = viewModel.User.UserName;
				newUser.Profile = viewModel.User.Profile;
				newUser.Secrets = new();
				newUser.Secrets.SetPassword(viewModel.NewPassword);

				await this.UserManager.Save(this.Context.Site, newUser);
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
				{
					return BadRequest("User name is already in use.");
				}
				else
				{
					throw;
				}
			}
			
			viewModel = await BuildViewModel("");
			viewModel.ShowForm = false;
			return Json(new { Title = "Sign Up", Message = "Your new account has been saved." });
			
		}

		private async Task<ViewModels.Signup> BuildViewModel(string returnUrl)
		{
			return new ViewModels.Signup()
			{
				ShowForm = true,
				ReturnUrl = returnUrl,
				User = await this.UserManager.CreateNew(this.Context.Site),
				ClaimTypeOptions=this.ClaimTypeOptions
			};
		}

	}
}
