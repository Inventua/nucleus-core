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
              // we can't use OnRetrieveLdapClaims.  See comments below regarding the Microsoft implementation (Microsoft.AspNetCore.Authentication.Negotiate)
              options.Events = new() { OnAuthenticated = HandleExternalAuthentication };
              //options.Events = new() { OnAuthenticated = HandleExternalAuthentication, OnRetrieveLdapClaims = HandleLdapClaims };

              // PersistKerberosCredentials is required in Linux.  If it is not present we get a "Interop+NetSecurityNative+GssApiException: GSSAPI operation
              // failed with error - Unspecified GSS failure", AFTER we have already authenticated (after HandleExternalAuthentication has
              // been called).  Looking at https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/Negotiate/src/NegotiateHandler.cs
              // line 95, it looks like by using PersistKerberosCredentials the NegotiateHandler keeps the negotiation state from a previous 
              // invocation & thus does not try to re-authenticate, which seems to cause the error.

              // We enable PersistKerberosCredentials for other operating system as well, because it improves performance and reduces network communication
              // between the web server running Nucleus and the KDC/Domain controller.
              options.PersistKerberosCredentials = true;

              // ---------------------------------------------------
              // Leave this commented-out code for reference.  The Microsoft implementation may improve to suit us better in the future.
              // ---------------------------------------------------
              // The Microsoft implementation (Microsoft.AspNetCore.Authentication.Negotiate) creates an LdapConnection object with settings which don't 
              // work properly in Linux, but if we create our own connection and call EnableLdap, the Microsoft implementation "validates" Ldap Settings 
              // during startup by doing an Ldap bind on the connection, which can take a long time or fail (during startup) and prevent Nucleus 
              // from starting.  If there is a failure, we want it to happen when a user tries to login with Windows Authentication rather than during 
              // startup.  In this case regular Nucleus authentication would still work, the user would get an error, and the error would be logged 
              // properly - none of these things happen if the application fails to start.

              // So we have to not call EnableLdap, and instead do our own Ldap claims resolution when we handle the OnAuthenticated event.  By not 
              // calling EnableLdap, the OnRetrieveLdapClaims event is not raised, and AuthenticationExtensions.HandleLdapClaims does not get called.

              //LdapConnection connection;
              //try
              //{
              //  connection = ExternalAuthenticationHandler.BuildLdapConnection(configuredProtocol, true, services.Logger());
              //}
              //catch (OperationCanceledException ex)
              //{
              //  services.Logger()?.LogError(ex, "The LdapConnection bind operation took more than 10 seconds to complete for scheme '{scheme}' '{domain}'.", configuredProtocol.Scheme, configuredProtocol.LdapDomain);
              //  connection = null;
              //}
              //catch (LdapException ex)
              //{
              //  services.Logger()?.LogError(ex, "Error building LdapConnection for scheme '{scheme}' '{domain}'.  Failed to connect to LDAP [{code}]. ", configuredProtocol.Scheme, configuredProtocol.LdapDomain, ex.ErrorCode);
              //  connection = null;
              //}
              //catch (Exception ex)
              //{
              //  services.Logger()?.LogError(ex, "Error building LdapConnection for scheme '{scheme}' '{domain}'", configuredProtocol.Scheme, configuredProtocol.LdapDomain);
              //  connection = null;
              //}

              //options.EnableLdap(settings =>
              //{
              //  settings.Domain = ExternalAuthenticationHandler.ResolveDomain(configuredProtocol, services.Logger());
              //  settings.LdapConnection = connection;
              //  settings.EnableLdapClaimResolution = true;
              //});              
              // ---------------------------------------------------

            });

            break;
        }
      }
    }

    return services;
  }

  /// <summary>
  /// Handler for the OnRetrieveLdapClaims event.
  /// </summary>
  /// <param name="context"></param>
  /// <returns></returns>
  /// <remarks>
  /// This function is not currently in use.  See comments abbove regarding the Microsoft implementation (Microsoft.AspNetCore.Authentication.Negotiate).
  /// Leave this for reference.  The Microsoft implementation may improve to suit us better in the future.
  /// </remarks>
#pragma warning disable IDE0051 // Remove unused private members
  private static async Task HandleLdapClaims(LdapContext context)
#pragma warning restore IDE0051 // Remove unused private members
  {
    ExternalAuthenticationHandler externalAuthenticationHandler = context.Request.HttpContext.RequestServices.GetService<ExternalAuthenticationHandler>();
    await externalAuthenticationHandler.HandleLdapClaims(context);
  }

  /// <summary>
  /// Handler for the OnAuthenticated event.  Calls the external authentication handler to create/synchronize data from Ldap and create a Nucleus
  /// session.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="context"></param>
  /// <returns></returns>
  private static async Task HandleExternalAuthentication<T>(ResultContext<T> context)
   where T : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
  {
    ExternalAuthenticationHandler externalAuthenticationHandler = context.Request.HttpContext.RequestServices.GetService<ExternalAuthenticationHandler>();
    await externalAuthenticationHandler.HandleExternalAuthentication(context, true);
  }
}
