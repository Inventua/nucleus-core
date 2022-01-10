using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Core;
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
		private UserManager UserManager { get; }
		private PageManager PageManager { get; }

		private ClaimTypeOptions ClaimTypeOptions { get; }

		public SignupController(Context context, ILogger<LoginController> Logger, UserManager userManager, PageManager pageManager, IOptions<ClaimTypeOptions> claimTypeOptions)
		{
			this.Context = context;
			this.Logger = Logger;
			this.UserManager = userManager;
			this.PageManager = pageManager;
			this.ClaimTypeOptions = claimTypeOptions.Value;
		}

		[HttpGet]
		public ActionResult Index(string returnUrl)
		{
			return View("Signup", BuildViewModel(returnUrl));
		}

		[HttpGet]
		public ActionResult Edit(string returnUrl)
		{
			return View("SignupSettings", new ViewModels.Signup());
		}

		[HttpPost]
		public ActionResult Signup(ViewModels.Signup viewModel)
		{
			if (!this.ModelState.IsValid)
			{
				return BadRequest(this.ModelState);
			}

			if (viewModel.ConfirmPassword != viewModel.NewPassword)
			{
				return BadRequest("Your new password and confirm password values do not match");
			}
			
			Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = this.UserManager.ValidatePasswordComplexity(viewModel.NewPassword);
			if (!modelState.IsValid)
			{
				return BadRequest(modelState);
			}
			
			try
			{
				User newUser = this.UserManager.CreateNew(this.Context.Site);

				newUser.UserName = viewModel.User.UserName;
				newUser.Profile = viewModel.User.Profile;
				newUser.Secrets = new();
				newUser.Secrets.SetPassword(viewModel.NewPassword);

				this.UserManager.Save(this.Context.Site, newUser);
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
			//return Redirect(String.IsNullOrEmpty(viewModel.ReturnUrl) ? "/" : viewModel.ReturnUrl);
			viewModel = BuildViewModel("");
			viewModel.ShowForm = false;
			viewModel.Message = "Your new account has been saved.";

			return View("Signup", viewModel);
		}

		private ViewModels.Signup BuildViewModel(string returnUrl)
		{
			return new ViewModels.Signup()
			{
				ControlId = $"_{Guid.NewGuid().ToString("N")}",
				ShowForm = true,
				ReturnUrl = returnUrl,
				User = this.UserManager.CreateNew(this.Context.Site),
				ClaimTypeOptions=this.ClaimTypeOptions
			};
		}

	}
}
