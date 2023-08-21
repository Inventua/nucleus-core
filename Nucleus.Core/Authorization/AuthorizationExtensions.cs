using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.Extensions;

namespace Nucleus.Core.Authorization
{
	public static class AuthorizationExtensions
	{
		/// <summary>
		/// If an unauthenticated user tries to view a page and does not have view permission, redirect to the site's login page, if the site 
		/// has a login page configured.
		/// </summary>
		/// <param name="app"></param>
		/// <returns></returns>
		public static IApplicationBuilder UseAuthorizationRedirect(this IApplicationBuilder app)
		{
			app.UseStatusCodePages(async context =>
			{
				if (!context.HttpContext.User.Identity.IsAuthenticated)
				{
					if (context.HttpContext.Response.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized || context.HttpContext.Response.StatusCode == (int)System.Net.HttpStatusCode.Forbidden)
					{
						if (!context.HttpContext.Request.Path.StartsWithSegments($"/{Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH}"))
						{
							IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();

							SitePages sitePages = context.HttpContext.RequestServices.GetService<Context>().Site.GetSitePages();
							PageRoute loginPageRoute = await GetPageRoute(sitePages.LoginPageId, pageManager);

							if (loginPageRoute != null)
							{
								string returnUrl;
																
								if (context.HttpContext.Request.Path.StartsWithSegments("/admin"))
								{
									// if we are processing a request to an endpoint in the admin UI, set the return Url to the site root
									returnUrl = context.HttpContext.Request.PathBase + "/";
								}
								else
								{
									// otherwise, the return Url is the requested endpoint uri
									returnUrl = context.HttpContext.Request.PathBase + context.HttpContext.Request.Path;
								}

								context.HttpContext.Response.Redirect($"{context.HttpContext.Request.PathBase}{loginPageRoute.Path}?returnUrl={ System.Uri.EscapeDataString(returnUrl)}");
							}
						}
					}
				}
			});

			return app;
		}

		private async static Task<PageRoute> GetPageRoute(Guid? pageId, IPageManager pageManager)
		{
			if (pageId.HasValue)
			{
				Page loginPage = await pageManager.Get(pageId.Value);
				if (loginPage != null)
				{
					return loginPage.DefaultPageRoute();
				}
			}

			return null;
		}

		/// <summary>
		/// Add Nucleus core Authorization, policies and handlers.
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static IServiceCollection AddCoreAuthorization(this IServiceCollection services)
		{
			services.UseCoreAuthorizationHandlers();

			// https://github.com/dotnet/AspNetCore.Docs/issues/6032
			// https://stackoverflow.com/questions/57234141/iauthorizationhandler-with-multiple-registration-how-the-dependency-resolver-s

			services.AddAuthorization(options => options.AddCorePolicies());

			return services;
		}

		private static AuthorizationOptions AddCorePolicies(this AuthorizationOptions options)
		{
			// new authorization handlers must be added here and in UseCoreAuthorizationHandlers
			options.AddPolicy(Nucleus.Abstractions.Authorization.Constants.PAGE_VIEW_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.PageViewPermissionAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.PageEditPermissionAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.SiteAdminAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.SystemAdminAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Abstractions.Authorization.Constants.SITE_WIZARD_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.SiteWizardAuthorizationRequirement());
			});

			return options;
		}

		private static IServiceCollection UseCoreAuthorizationHandlers(this IServiceCollection services)
		{
			// https://stackoverflow.com/questions/57234141/iauthorizationhandler-with-multiple-registration-how-the-dependency-resolver-s
			services.AddScoped<IAuthorizationHandler, Nucleus.Core.Authorization.PageViewPermissionAuthorizationHandler>();
			services.AddScoped<IAuthorizationHandler, Nucleus.Core.Authorization.PageEditPermissionAuthorizationHandler>();
			services.AddScoped<IAuthorizationHandler, Nucleus.Core.Authorization.SiteAdminAuthorizationHandler>();
			services.AddScoped<IAuthorizationHandler, Nucleus.Core.Authorization.SystemAdminAuthorizationHandler>();
			services.AddScoped<IAuthorizationHandler, Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler>();
			services.AddScoped<IAuthorizationHandler, Nucleus.Core.Authorization.SiteWizardAuthorizationHandler>();

			return services;
		}
	}
}
