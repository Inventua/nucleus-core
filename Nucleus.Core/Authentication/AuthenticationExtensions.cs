using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Core.Authentication
{
	public static class AuthenticationExtensions
	{
		

		/// <summary>
		/// Add Nucleus core Authentication to DI.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		/// <remarks>
		/// Configures and adds Nucleus core authentication to DI, including the <see cref="AuthenticationOptions"/> class.
		/// </remarks>
		public static IServiceCollection AddCoreAuthentication(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<Authentication.AuthenticationOptions>(configuration.GetSection(Authentication.AuthenticationOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);

			services.AddAuthentication(Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME)
				.AddScheme<Nucleus.Core.Authentication.AuthenticationOptions, Nucleus.Core.Authentication.AuthenticationHandler>(Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME, options =>
				{	});

			return services;
		}
	}
}
