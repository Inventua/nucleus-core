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
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;

// REFERENCES:
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.iauthenticationservice?view=aspnetcore-6.0
// https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/blob/dev/src/AspNet.Security.OAuth.WordPress/WordPressAuthenticationExtensions.cs 
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.oauth.oauthmiddleware-1?view=aspnetcore-1.1
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1?view=aspnetcore-1.1

namespace Nucleus.OAuth.Client.Controllers
{
	[Extension("OAuthClient")]
	public class OAuthClientController : Controller
	{
		private IWebHostEnvironment WebHostEnvironment { get; }

		private Context Context { get; }
		
		private ISiteManager SiteManager { get; }

		private IPageModuleManager PageModuleManager { get; }

		private IOptions<Models.Configuration.OAuthProviders> Options { get; }
		private ILogger<OAuthClientAdminController> Logger { get; }

		public OAuthClientController(IWebHostEnvironment webHostEnvironment, Context Context, ISiteManager siteManager, IPageModuleManager pageModuleManager, IOptions<Models.Configuration.OAuthProviders> options, ILogger<OAuthClientAdminController> logger)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.SiteManager = siteManager;
			this.PageModuleManager = pageModuleManager;
			this.Options = options;
			this.Logger = logger;
		}

		[HttpGet]
		public ActionResult Index(string returnUrl)
		{
			ViewModels.Viewer viewModel = BuildViewModel(returnUrl);
			if (viewModel.AutoLogin)
			{
				if (!User.Identity.IsAuthenticated && this.Options.Value.Count == 1)
				{
					Models.Configuration.OAuthProvider providerOption = this.Options.Value.FirstOrDefault();

					if (providerOption != null)
					{
						string url = BuildRedirectUrl(returnUrl);
						Logger?.LogTrace("OAuth Provider Selector: AutoLogin is enabled, automatically redirecting to '{url}'.", url);
						// redirect to OAUTH provider 
						return Challenge(new AuthenticationProperties() { RedirectUri = url }, providerOption.Name ?? providerOption.Type);
					}
				}
			}

			return View("Viewer", viewModel);
		}

		private string BuildRedirectUrl(string returnUrl)
		{
			// Only allow a relative path for redirectUri (that is, the url must start with "/"), to ensure that it points to "this" site.					
			return Url.Content(String.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/") ? "~/" : returnUrl);
		}

		/// <summary>
		/// Start a remote login by redirecting to the specified remote (OAuth) provider.  
		/// </summary>
		/// <param name="providerName"></param>
		/// <param name="returnUrl"></param>
		/// <returns></returns>
		/// <remarks>
		/// The remote login is started by returning Challenge(options, providerName).  The aspnet core authentication middleware responds to this by 
		/// redirecting to the provider's AuthorizationEndpoint.
		/// </remarks>
		[HttpGet]
		[Route($"/{RoutingConstants.EXTENSIONS_ROUTE_PATH}/{{extension=OAuthClient}}/{{action=Authenticate}}/{{providerName}}")]
		public ActionResult Authenticate(string providerName, string returnUrl)
		{
			if (!String.IsNullOrEmpty(providerName))
			{
				Logger?.LogTrace("OAUTH provider {providername} requested.", providerName);

				// Find provider configuration matching the supplied providerName.  The Name property is optional, but takes precendence over the Type.  If the name is not specifed, match by Type.
				// It is possible to have multiple OpenIdConnect configurations which have settings which point to different OAUTH providers, which are identified by Name.  Most of the settings for the
				// Facebook/Google/Twitter/MicrosoftAccount are built in to the Microsoft.AspNetCore.Authentication.* package, so they can only have one configuration set up, and do require a name.
				Models.Configuration.OAuthProvider providerOption = this.Options.Value
					.Where(option => (option.Name != null && option.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase)) || option.Type.Equals(providerName, StringComparison.OrdinalIgnoreCase))
					.FirstOrDefault();

				if (providerOption != null)
				{
					string url = BuildRedirectUrl(returnUrl);
					Logger?.LogTrace("Starting the remote login process.");
					// redirect to OAUTH provider 
					return Challenge(new AuthenticationProperties() { RedirectUri = url }, providerOption.Name ?? providerOption.Type);
				}
				else
				{
					Logger?.LogTrace("OAUTH provider {providername} not found.  Check your configuration files Nucleus:OAuthProviders section for a provider with a matching name, or a matching type and no name.", providerName);
					return BadRequest();
				}
			}
			else
			{
				Logger?.LogTrace("No provider name was specified.  The Url for remote logins is https:/[your-domain]/extensions/OAuthClient/Authenticate/providerName, where providerName matches a configuration file Nucleus:OAuthProviders section for a provider with a matching name, or a matching type and no name.");
				return BadRequest();
			}			
		}


		private ViewModels.Viewer BuildViewModel(string redirectUri)
		{
			ViewModels.Viewer viewModel = new();
			viewModel.Options = this.Options.Value;
			viewModel.ReturnUrl = redirectUri;

			// Name property is optional in config - for empty names, set the name to the value of the type property so that we
			// don't have to check for blank names in views.  
			foreach (Models.Configuration.OAuthProvider option in viewModel.Options)
			{
				if (String.IsNullOrEmpty(option.Name))
				{
					option.Name = option.Type;
				}
			}

			viewModel.ReadSettings(this.Context.Module);

			string layoutPath = $"ViewerLayouts\\{viewModel.Layout}.cshtml";

			if (!System.IO.File.Exists($"{this.WebHostEnvironment.ContentRootPath}\\{FolderOptions.EXTENSIONS_FOLDER}\\OAuth Client\\Views\\{layoutPath}"))
			{
				layoutPath = $"ViewerLayouts\\List.cshtml";
			}

			viewModel.Layout = layoutPath;

			return viewModel;
		}



	}
}