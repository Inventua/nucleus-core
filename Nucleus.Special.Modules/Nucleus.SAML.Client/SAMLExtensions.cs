using System.Linq;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Extensions.Logging;

namespace Nucleus.SAML.Client
{
  internal static class SAMLExtensions
	{
		public static void AddSAML(this AuthenticationBuilder builder, IConfiguration configuration)
		{
      // Add config to dependency injection 
			builder.Services.Configure<Models.Configuration.SAMLProviders>(configuration.GetSection(Models.Configuration.SAMLProviders.Section), options => options.BindNonPublicProperties = true);

			builder.Services.AddSaml2(slidingExpiration: true);
			builder.Services.AddHttpClient();
		}
	}
}
