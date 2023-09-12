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
using Nucleus.Abstractions.Authentication;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using Nucleus.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nucleus.Core.Authentication;

//
// Kestrel can only support binding to an IP address/port, not a host name.
//

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
    services.AddOption<AuthenticationOptions>(configuration, AuthenticationOptions.Section);
    services.AddOption<AuthenticationProtocols>(configuration, AuthenticationProtocols.Section);

    AuthenticationBuilder authBuilder = services.AddAuthentication(options =>
    {
      options.DefaultScheme = Constants.DEFAULT_AUTH_SCHEME;

      options.DefaultSignInScheme = Constants.DEFAULT_AUTH_SCHEME;
      options.DefaultSignOutScheme = Constants.DEFAULT_AUTH_SCHEME;
      options.DefaultForbidScheme = Constants.DEFAULT_AUTH_SCHEME;

      options.DefaultAuthenticateScheme = Constants.DEFAULT_AUTH_SCHEME;
      options.DefaultChallengeScheme = Constants.DEFAULT_AUTH_SCHEME;
    });

    authBuilder.AddScheme<AuthenticationOptions, AuthenticationHandler>(Constants.DEFAULT_AUTH_SCHEME, Constants.DEFAULT_AUTH_DISPLAY_NAME, options => { });

    services.AddScoped<ExternalAuthenticationHandler>();

    // read configuration for external authentication protocols
    AuthenticationProtocols authenticationProtocols = configuration.GetSection(Nucleus.Abstractions.Models.Configuration.AuthenticationProtocols.Section)
      .Get<AuthenticationProtocols>(binderOptions => binderOptions.BindNonPublicProperties = true);

    if (authenticationProtocols != null)
    {
      // add configured external authorization protocols.  This code only supports the Negotiate (Windows Authentication/Kerberos) protocol for now.
      foreach (AuthenticationProtocol configuredProtocol in authenticationProtocols.Where(proto => proto.Enabled))
      {
        switch (configuredProtocol.Scheme)
        {
          case NegotiateDefaults.AuthenticationScheme:
            authBuilder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, configuredProtocol.FriendlyName, options =>
            {
              options.Events = new() { OnAuthenticated = HandleExternalAuthentication, OnRetrieveLdapClaims = HandleLdapClaims };
              
              // PersistKerberosCredentials is required in Linux.  If it is not present we get a "Interop+NetSecurityNative+GssApiException: GSSAPI operation
              // failed with error - Unspecified GSS failure", AFTER we have already authenticated (after HandleExternalAuthentication has
              // been called).  Looking at https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/Negotiate/src/NegotiateHandler.cs
              // line 95, it looks like by using PersistKerberosCredentials the NegotiateHandler keeps the negotiation state from a previous 
              // invocation & thus does not try to re-authenticate, which seems to cause the error.

              // We enable PersistKerberosCredentials for other operating system as well, because it improves performance and reduces network communication
              // between the web server running Nucleus and the KDC/Domain controller.
              options.PersistKerberosCredentials = true;
              
              LdapConnection connection;
              try
              {
                connection = ExternalAuthenticationHandler.BuildLdapConnection(configuredProtocol, services.Logger());
              }
              catch (LdapException ex)
              {
                services.Logger()?.LogError(ex, "Error building LdapConnection for scheme '{scheme}' '{domain}'.  Failed to connect to LDAP [{code}]. ", configuredProtocol.Scheme, configuredProtocol.LdapDomain, ex.ErrorCode);
                connection = null;
              }
              catch (Exception ex)
              {
                services.Logger()?.LogError(ex, "Error building LdapConnection for scheme '{scheme}' '{domain}'", configuredProtocol.Scheme, configuredProtocol.LdapDomain);
                connection = null;
              }

              if (connection != null)
              {
                options.EnableLdap(settings =>
                {
                  string domain = (connection.Directory as LdapDirectoryIdentifier).Servers.FirstOrDefault();
                  settings.Domain = domain;
                  settings.LdapConnection = connection;
                  settings.EnableLdapClaimResolution = true;
                });
              }

            });

            break;
        }
      }
    }

    return services;
  }

  private static async Task HandleLdapClaims(LdapContext context)
  {
    ExternalAuthenticationHandler externalAuthenticationHandler = context.Request.HttpContext.RequestServices.GetService<ExternalAuthenticationHandler>();
    await externalAuthenticationHandler.HandleLdapClaims(context);
  }


  private static async Task HandleExternalAuthentication<T>(ResultContext<T> context)
   where T : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
  {
    ExternalAuthenticationHandler externalAuthenticationHandler = context.Request.HttpContext.RequestServices.GetService<ExternalAuthenticationHandler>();
    await externalAuthenticationHandler.HandleExternalAuthentication(context);
  }
}
