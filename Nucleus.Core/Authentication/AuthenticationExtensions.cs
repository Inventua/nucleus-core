using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.AspNetCore.Authentication.Negotiate;
using System.DirectoryServices.Protocols;

namespace Nucleus.Core.Authentication;

public static class AuthenticationExtensions
{
  /// <summary>
  /// Add Nucleus core Authentication and additional authentication protocols that Nucleus supports and are configured.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  /// <remarks>
  /// Configures and adds Nucleus core authentication to DI, including the <see cref="AuthenticationOptions"/> class.
  /// </remarks>
  public static IServiceCollection AddCoreAuthentication(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddOption<Authentication.AuthenticationOptions>(configuration, Authentication.AuthenticationOptions.Section);
    services.AddOption<AuthenticationProtocols>(configuration, AuthenticationProtocols.Section);

    //services.AddAuthentication(Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME)
    //  .AddScheme<Nucleus.Core.Authentication.AuthenticationOptions, Nucleus.Core.Authentication.AuthenticationHandler>(Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME, options => { });
    services.AddAuthentication(options =>
    {
      options.DefaultScheme = Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME;
      options.DefaultSignInScheme = Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME;
      options.DefaultSignOutScheme = Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME;
      options.DefaultForbidScheme = Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME;
      options.DefaultAuthenticateScheme = Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME;
      options.DefaultChallengeScheme = Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME;
    })
    .AddScheme<Nucleus.Core.Authentication.AuthenticationOptions, Nucleus.Core.Authentication.AuthenticationHandler>(Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME, options => { });

    services.AddScoped<ExternalAuthenticationHandler>();

    // read configuration for external authentication protocols
    AuthenticationProtocols authenticationProtocols = configuration.GetSection(Nucleus.Abstractions.Models.Configuration.AuthenticationProtocols.Section)
      .Get<AuthenticationProtocols>(binderOptions => binderOptions.BindNonPublicProperties = true);

    if (authenticationProtocols != null)
    {
      // initialize LDAP connections
      ExternalAuthenticationHandler.InitializeLDAP(services, authenticationProtocols);

      // add configured external authorization protocols
      foreach (AuthenticationProtocol configuredProtocol in authenticationProtocols.Where(proto => proto.Enabled))
      {
        switch (configuredProtocol.Scheme)
        {
          case NegotiateDefaults.AuthenticationScheme:
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
              .AddNegotiate(options =>
              {
                options.Events = new() { OnAuthenticated = HandleExternalAuthentication };

                if (!String.IsNullOrEmpty(configuredProtocol.Domain))
                {
                  LdapConnection connection = ExternalAuthenticationHandler.GetConnection(configuredProtocol.Scheme);

                  if (connection != null)
                  {
                    options.EnableLdap(settings =>
                    {
                      // we create our own connection to LDAP so that we can share the connection between Authentication.Negotiate and
                      // ExternalAuthenticationHandler.
                      settings.LdapConnection = connection;
                      settings.Domain = configuredProtocol.Domain;
                      //settings.EnableLdapClaimResolution = true;
                    });
                  }
                }
              });

            break;
        }
      }
    }

    return services;
  }

  private static async Task HandleExternalAuthentication<T>(ResultContext<T> context)
   where T : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
  {
    ExternalAuthenticationHandler externalAuthenticationHandler = context.Request.HttpContext.RequestServices.GetService<ExternalAuthenticationHandler>();
    await externalAuthenticationHandler.HandleExternalAuthentication(context);
  }
}
