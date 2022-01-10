using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Core.Authorization
{
	public static class AuthorizationExtensions
	{
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
			options.AddPolicy(Nucleus.Core.Authorization.PageViewPermissionAuthorizationHandler.PAGE_VIEW_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.PageViewPermissionAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Core.Authorization.PageEditPermissionAuthorizationHandler.PAGE_EDIT_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.PageEditPermissionAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.SiteAdminAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.SystemAdminAuthorizationRequirement());
			});

			options.AddPolicy(Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY, policy =>
			{
				policy.Requirements.Add(new Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationRequirement());
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

			return services;
		}
	}
}
