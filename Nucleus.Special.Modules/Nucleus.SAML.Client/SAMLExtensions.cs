using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Nucleus.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;

namespace Nucleus.SAML.Client
{
	internal static class SAMLExtensions
	{
		private static string[] ReservedProperties = GetReservedProperties();

		private static string[] GetReservedProperties()
		{
			System.Reflection.PropertyInfo[] properties = typeof(Models.Configuration.SAMLProvider).GetProperties();

			return properties.Select(prop => prop.Name).ToArray();

		}

		///// <summary>
		///// Add Nucleus core Authentication to DI.
		///// </summary>
		///// <param name="services"></param>
		///// <param name="configuration"></param>
		///// <returns></returns>
		///// <remarks>
		///// Configures and adds Nucleus remote (SAML) authentication to DI, including the <see cref="AuthenticationOptions"/> class.
		///// </remarks>
		//public static IServiceCollection AddRemoteAuthentication(this IServiceCollection services, IConfiguration configuration)
		//{
		//	//services.Configure<RemoteAuthenticationOptions>(configuration.GetSection(RemoteAuthenticationOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);
			
		//	//services.AddAuthentication(Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME)
		//	//	.AddScheme<RemoteAuthenticationOptions, RemoteAuthenticationHandler>(RemoteAuthenticationHandler.REMOTE_AUTH_SCHEME, options =>
		//	//	{
					
		//	//	});

		//	return services;
		//}

		public static void AddSAML(this AuthenticationBuilder builder, IConfiguration configuration)
		{			
			//AddRemoteAuthentication(builder.Services, configuration);
				
			// Look for configured SAML providers

			// Get config for immediate use
			Models.Configuration.SAMLProviders config = new();
			configuration.Bind(Models.Configuration.SAMLProviders.Section, config);

			// Add config to dependency injection for later use
			builder.Services.Configure<Models.Configuration.SAMLProviders>(configuration.GetSection(Models.Configuration.SAMLProviders.Section), options => options.BindNonPublicProperties = true);

			builder.Services.AddSaml2(slidingExpiration: true);
			builder.Services.AddHttpClient();
		}
	}
}
