using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.AspNetCore.Hosting;

// https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.iauthenticationservice?view=aspnetcore-6.0
// https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/blob/dev/src/AspNet.Security.OAuth.WordPress/WordPressAuthenticationExtensions.cs
// https://github.com/aspnet/Security/blob/master/src/Microsoft.AspNetCore.Authentication.OAuth/OAuthExtensions.cshttps://github.com/aspnet/Security/blob/master/src/Microsoft.AspNetCore.Authentication.OAuth/OAuthExtensions.cs
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.oauth.oauthmiddleware-1?view=aspnetcore-1.1

// https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1?view=aspnetcore-1.1

namespace Nucleus.OAuth.Controllers
{
	[Extension("OAuth")]
	public class OAuthController : Controller
	{
		private IWebHostEnvironment WebHostEnvironment { get; }

		private Context Context { get; }
		
		private ISiteManager SiteManager { get; }

		private IOptions<OAuth.Models.Configuration.OAuthProviders> Options { get; }

		private const string MODULESETTING_LAYOUT = "oauth:viewer:layout";

		public OAuthController(IWebHostEnvironment webHostEnvironment, Context Context, ISiteManager siteManager, IOptions<OAuth.Models.Configuration.OAuthProviders> options)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.SiteManager = siteManager;
			this.Options = options;
		}

		[HttpGet]
		//[Route($"/{RoutingConstants.EXTENSIONS_ROUTE_PATH}/{{extension:exists}}/{{action=Index}}/{{provider}}")]
		public ActionResult Index(string returnUrl)
		{
			return View("Viewer", BuildViewModel(returnUrl));
		}

		[HttpGet]
		[Route($"/{RoutingConstants.EXTENSIONS_ROUTE_PATH}/{{extension:exists}}/{{action=AccessDenied}}")]
		public ActionResult AccessDenied()
		{
			return View("Viewer", BuildViewModel(""));
		}

		[HttpGet]
		[Route($"/{RoutingConstants.EXTENSIONS_ROUTE_PATH}/{{extension:exists}}/{{action=Authenticate}}/{{providerName}}")]
		public ActionResult Authenticate(string providerName, string returnUrl)
		{
			if (!String.IsNullOrEmpty(providerName))
			{
				// Find provider configuration matching the supplied providerName.  The Name property is optional, but takes precendence over the Type.  If the name is not specifed, match by Type.
				// It is possible to have multiple OpenIdConnect configurations which have settings which point to different OAUTH providers, which are identified by Name.  Most of the settings for the
				// Facebook/Google/Twitter/MicrosoftAccount are built in to the Microsoft.AspNetCore.Authentication.* package, so they can only have one configuration set up, and do require a name.
				OAuth.Models.Configuration.OAuthProvider providerOption = this.Options.Value
					.Where(option => (option.Name != null && option.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase)) || option.Type.Equals(providerName, StringComparison.OrdinalIgnoreCase))
					.FirstOrDefault();

				if (providerOption != null)
				{
					// redirect to OAUTH provider 
					// Only allow a relative path for redirectUri, to ensure that it points to "this" site.					
					return Challenge(new AuthenticationProperties() { RedirectUri = String.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/") ? "/" : returnUrl }, providerOption.Name ?? providerOption.Type);
					//return Challenge(new AuthenticationProperties() { RedirectUri = String.IsNullOrEmpty(redirectUri) || !redirectUri.StartsWith("/") ? "/" : redirectUri }, $"{RemoteAuthenticationHandler.REMOTE_AUTH_SCHEME}/{providerName}");
				}
				else
				{
					return BadRequest();
				}
			}
			else
			{
				return BadRequest();
			}			
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult SiteSettings(ViewModels.SiteSettings viewModel)
		{
			return View("SiteSettings", BuildSiteSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SaveSettings(ViewModels.SiteSettings viewModel)
		{
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteSettings.SETTING_MATCH_BY_NAME, viewModel.MatchByName);
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteSettings.SETTING_MATCH_BY_EMAIL, viewModel.MatchByEmail);

			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteSettings.SETTING_CREATE_USERS, viewModel.CreateUsers);
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteSettings.SETTING_AUTO_VERIFY, viewModel.AutomaticallyVerifyNewUsers);
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteSettings.SETTING_AUTO_APPROVE, viewModel.AutomaticallyApproveNewUsers);

			this.SiteManager.Save(this.Context.Site);

			return Ok();
		}

		private ViewModels.Viewer BuildViewModel(string redirectUri)
		{
			ViewModels.Viewer viewModel = new();
			viewModel.Options = this.Options.Value;
			viewModel.ReturnUrl = redirectUri;

			// Name property is optional in config - for empty names, set the name to the value of the type property so that we
			// don't have to check for blank names in views.  
			foreach (OAuth.Models.Configuration.OAuthProvider option in viewModel.Options)
			{
				if (String.IsNullOrEmpty(option.Name))
				{
					option.Name = option.Type;
				}
			}

			string layoutPath = $"ViewerLayouts\\{this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "List")}.cshtml";

			if (!System.IO.File.Exists($"{this.WebHostEnvironment.ContentRootPath}\\{FolderOptions.EXTENSIONS_FOLDER}\\OAuth\\Views\\{layoutPath}"))
			{
				layoutPath = $"ViewerLayouts/List.cshtml";
			}

			viewModel.Layout = layoutPath;

			return viewModel;
		}

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "List");

			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{FolderOptions.EXTENSIONS_FOLDER}\\OAuth\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}

			return viewModel;
		}

		private ViewModels.SiteSettings BuildSiteSettingsViewModel(ViewModels.SiteSettings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ReadSettings(this.Context.Site);

			return viewModel;
		}

	}
}