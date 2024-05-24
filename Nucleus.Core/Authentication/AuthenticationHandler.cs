using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Routing;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Authentication
{
  public class AuthenticationHandler : SignInAuthenticationHandler<AuthenticationOptions>
  {
    private ISessionManager SessionManager { get; }
    private IUserManager UserManager { get; }
    private ISiteManager SiteManager { get; }
    private IApiKeyManager ApiKeyManager { get; }

    private Context CurrentContext { get; set; }
    private LinkGenerator LinkGenerator { get; }

    new AuthenticationOptions Options { get; }


    public AuthenticationHandler(ISessionManager sessionManager, IUserManager userManager, IApiKeyManager apiKeyManager, LinkGenerator linkGenerator, ISiteManager siteManager,
                                 Context context,
                                 IOptionsMonitor<AuthenticationOptions> optionsMonitor,
                                 ILoggerFactory logger,
                                 System.Text.Encodings.Web.UrlEncoder encoder) : base(optionsMonitor, logger, encoder)
    {
      this.SessionManager = sessionManager;
      this.UserManager = userManager;
      this.SiteManager = siteManager;
      this.ApiKeyManager = apiKeyManager;
      this.LinkGenerator = linkGenerator;
      this.CurrentContext = context;

      // This is a workaround to allow authentication options to be set in configuration files.  AuthenticationBuilder.AddScheme uses "named options" to set the
      // .Options property, which prevents our config settings from being loaded.
      // https://github.com/dotnet/aspnetcore/issues/17539 
      // https://github.com/aspnet/AspNetCore/blob/3b7cdc166aa3c9733fa60b56d91fc0fff9b11652/src/Security/Authentication/Core/src/AuthenticationHandler.cs#L85
      this.Options = this.OptionsMonitor.CurrentValue;
    }

    /// <summary>
    /// Handle Authentication
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// Retrieve the Session ID from the session cookie, update the cookie and user session expiry date if sliding expiration is enabled,
    /// set user claims used by the rest of Nucleus.
    /// </remarks>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
      string sessionId = this.Context.Request.Cookies[this.Options.CookieName];

      if (String.IsNullOrEmpty(sessionId))
      {
        if (this.Context.Request.IsSigned(out Guid accessKey))
        {
          // handle request with an API key-derived signature
          return await HandleApiRequest(accessKey);
        }
        else
        {
          // user is not logged in
          Logger.LogTrace("Session cookie blank or not present.");
          return await HandleUnauthenticatedRequest();
        }
      }
      else
      {
        // handle a signed in user
        return await HandleUserRequest(sessionId);
      }
    }

    private async Task<AuthenticateResult> HandleUserRequest(string sessionId)
    {
      List<Claim> claims = new();
      ClaimsIdentity identity;
      ClaimsPrincipal principal;

      UserSession userSession = null;

      Logger.LogTrace("Reading session {sessionId}.", sessionId);

      try
      {
        userSession = await this.SessionManager.Get(Guid.Parse(sessionId));
      }
      catch (Exception e)
      {
        Logger?.LogError(e, "");
        // we need to suppress exceptions here, because when a database connection failure occurs, the Authentication handler (this class) is
        // called, which causes the exception to be thrown again, which disrupts the error handler.
      }

      if (userSession != null)
      {
        // user session exists

        // enforce "same ip address", if configured.  Same IP address validation can cause problems for users on a VPN or roaming connection,
        // because their IP address can change.  If this happens, they get logged out.  The default value for EnforceSameIPAddress is false.
        if (this.Options.EnforceSameIPAddress && !IsEqual(userSession.RemoteIpAddress, this.Context.Connection.RemoteIpAddress))
        {
          Logger.LogCritical("User {UserId} attempted to use a session {SessionId} from {CurrentRemoteIpAddress} when the original session was from {OriginalRemoteIpAddress} and has been logged out.", userSession.UserId, userSession.Id, this.Context.Connection.RemoteIpAddress, userSession.RemoteIpAddress);
          await this.SessionManager.Delete(userSession);
          _ = this.SessionManager.SignOut(this.Context);
          return AuthenticateResult.Fail("Access Denied");
        }

        // update sliding expiration in the database, and update the cookie expiry date
        if (userSession.ExpiryDate > DateTime.UtcNow)
        {
          // session has not expired
          if (userSession.SlidingExpiry)
          {
            Logger.LogTrace("Session {sessionId} is valid, updating sliding expiration.", sessionId);

            userSession.ExpiryDate = DateTime.UtcNow.Add(this.Options.SlidingExpirationTimeSpan);

            // Only update the database expiry date if the database was updated more than 1 minute ago.  This is to avoid a negative impact
            // on performance, as the authentication handler is called for every request - even static files - so a single page load could otherwise
            // generate dozens of database updates.
            if (!userSession.LastUpdated.HasValue || DateTime.UtcNow > userSession.LastUpdated.Value.AddSeconds(60))
            {
              userSession.LastUpdated = DateTime.UtcNow;
              await this.SessionManager.Save(userSession);
            }

            AppendCookie(userSession.Id.ToString(), this.SessionManager.BuildAuthenticationProperties(userSession));
          }
        }
        else
        {
          // Session has expired
          Logger.LogTrace("Session has expired.");
          await this.SessionManager.Delete(userSession);
          _ = this.SessionManager.SignOut(this.Context);
          return AuthenticateResult.Fail("Session Expired");
        }
      }
      else
      {
        // user session was not found in the database (or cache)
        Logger.LogWarning("Invalid session Id {sessionId} sent from {remoteIpAddress}.", sessionId, this.Context.Connection.RemoteIpAddress);
      }

      if (userSession == null)
      {
        // remove expired cookie
        this.Context.Response.Cookies.Delete(this.Options.CookieName);

        // invalid session ID, treat the user as not logged in
        return await HandleUnauthenticatedRequest();
      }
      else
      {
        // session is valid, set up user identity
        User user = await this.UserManager.Get(await this.SiteManager.Get(userSession.SiteId), userSession.UserId);

        if (user == null)
        {
          Logger.LogWarning("User Id '{userId}' for Session Id '{sessionId}' was not found in the database.", userSession.UserId, sessionId);
          return AuthenticateResult.Fail("Access Denied");
        }
        else
        {
          Logger.LogTrace("User Id '{userId}' was found for session Id '{sessionId}'.", userSession.UserId, sessionId);

          claims.Add(new Claim(ClaimTypes.AuthenticationMethod, Nucleus.Abstractions.Authentication.Constants.AUTHENTICATED_BY_COOKIE));

          if (!user.Approved || !user.Verified)
          {
            // Users who are not approved/verified are still authenticated (we know who they are), but they do not get any roles, and 
            // if they are site admins or system admins, they don't get those claims added, so the authorization system doesn't allow them
            // to access any protected functionality.            
            if (!user.Approved)
            {
              Logger.LogInformation("Limiting access for session Id '{sessionId}' / user Id '{userId}' - user not approved.", sessionId, userSession.UserId);
              // The "not approved" claim is added to make it easier to check for in other parts of the system so that we can present messages to warn the user
              claims.Add(new Claim(Nucleus.Abstractions.Authentication.Constants.NOT_APPROVED_CLAIMTYPE, ""));
            }
            else if (!user.Verified)
            {
              Logger.LogInformation("Limiting access for session Id '{sessionId}' / user Id '{userId}' - user not verified.", sessionId, userSession.UserId);
              // The "not verified" claim is added to make it easier to check for in other parts of the system so that we can present messages to warn the user
              claims.Add(new Claim(Nucleus.Abstractions.Authentication.Constants.NOT_VERIFIED_CLAIMTYPE, ""));
            }

            // add basic identity claims only
            Logger.LogTrace("User Id '{userId}': Adding Claim '{claimType}': '{userId}'.", userSession.UserId, ClaimTypes.NameIdentifier, user.Id);
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            Logger.LogTrace("User Id '{userId}': Adding Claim '{claimType}': '{userName}'.", userSession.UserId, ClaimTypes.Name, user.UserName);
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
          }
          else
          {
            // User was found and is approved and verified
            Logger.LogTrace("User Id '{userId}' was found for session Id '{sessionId}': Adding Claims.", userSession.UserId, sessionId);

            // add basic identity claims
            Logger.LogTrace("User Id '{userId}': Adding Claim '{claimType}': '{userId}'.", userSession.UserId, ClaimTypes.NameIdentifier, user.Id);
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            Logger.LogTrace("User Id '{userId}': Adding Claim '{claimType}': '{userName}'.", userSession.UserId, ClaimTypes.Name, user.UserName);
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));

            // if the user is a system administrator, add the system administrator claim 
            if (user.IsSystemAdministrator)
            {
              Logger.LogTrace("User Id '{userId}': Adding Claim '{claimType}'.", userSession.UserId, Nucleus.Abstractions.Authentication.Constants.SYSADMIN_CLAIMTYPE);
              claims.Add(new Claim(Nucleus.Abstractions.Authentication.Constants.SYSADMIN_CLAIMTYPE, ""));
            }

            // add role claims
            if (user.Roles != null)
            {
              foreach (Role role in user.Roles)
              {
                Logger.LogTrace("User Id '{userId}': Adding Claim '{claimType}' '{roleName}'.", userSession.UserId, ClaimTypes.Role, role.Name);
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
              }
            }

            // add user profile properties as claims
            foreach (UserProfileValue profileValue in user.Profile)
            {
              if (!String.IsNullOrWhiteSpace(profileValue.Value) && !String.IsNullOrEmpty(profileValue?.UserProfileProperty.TypeUri))
              {
                Logger.LogTrace("User Id '{userId}': Adding Claim '{claimType}' '{claimValue}'.", userSession.UserId, profileValue.UserProfileProperty.TypeUri, profileValue.Value);
                claims.Add(new Claim(profileValue.UserProfileProperty.TypeUri, profileValue.Value));
              }
            }

            if (user.Secrets?.PasswordExpiryDate < DateTime.UtcNow)
            {
              // when a user's password has expired, they still need roles and other claims so that they can access the "change password" page.  Users are 
              // prevented from accessing any other pages by Nucleus.Web.Controllers.DefaultController, which checks for PASSWORD_EXPIRED_CLAIMTYPE.
              Logger.LogInformation("User Id '{userId}': Limiting access for session Id '{sessionId}' - user password has expired.", userSession.UserId, sessionId);

              // The "expired password" claim is added to make it easier to check for in other parts of the system so that we can present messages to warn the user and 
              // force them to change their password.  Users with expired password must get their normal roles so that they can access the "change password" page.
              claims.Add(new Claim(Nucleus.Abstractions.Authentication.Constants.PASSWORD_EXPIRED_CLAIMTYPE, ""));
            }
          }
        }

        identity = new ClaimsIdentity(claims, Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME);
        principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME));
      }
    }

    private async Task<AuthenticateResult> HandleApiRequest(Guid accessKey)
    {
      ApiKey apiKey = await this.ApiKeyManager.Get(accessKey);
      if (apiKey == null)
      {
        Logger.LogWarning("Invalid Api Key '{accessKey}' from '{remoteIpAddress}'.", accessKey, this.Context.Connection.RemoteIpAddress);
        return AuthenticateResult.Fail($"Invalid Api Key '{accessKey}' from '{this.Context.Connection.RemoteIpAddress}'.");
      }
      else if (!apiKey.Enabled)
      {
        Logger.LogWarning("Invalid [disabled] Api Key '{accessKey}' from '{remoteIpAddress}'.", accessKey, this.Context.Connection.RemoteIpAddress);
        return AuthenticateResult.Fail($"Invalid [disabled] Api Key '{accessKey}' from '{this.Context.Connection.RemoteIpAddress}'.");
      }
      else
      {
        if (!this.Context.Request.IsValid(apiKey.Secret, out string reason))
        {
          Logger.LogWarning("Invalid Api Signature [Access Key: '{accessKey}'] from '{remoteIpAddress}', '{reason}'.", accessKey, this.Context.Connection.RemoteIpAddress, reason);
          return AuthenticateResult.Fail($"Invalid Api Signature [Access Key: '{accessKey}'] from '{this.Context.Connection.RemoteIpAddress}'.");
        }
        else
        {
          List<Claim> claims = new();
          ClaimsIdentity identity;
          ClaimsPrincipal principal;

          // Signature is valid.  Apply scope values to identity claims.  We do not set a session cookie for API calls.
          claims.Add(new Claim(ClaimTypes.AuthenticationMethod, Nucleus.Extensions.HttpRequestExtensions.AUTHORIZATION_SCHEME));
          claims.Add(new Claim(ClaimTypes.NameIdentifier, apiKey.Id.ToString()));

          // Scopes are CRLF separated.
          // Each scope can contain a claimtype:claimvalue.  A scope can  (theoretically) contain a single value (no ':'),
          // or multiple values separated by (':'), or something else: But if they do, the scope is ignored by this code and would have to
          // be checked by some other component.
          foreach (string scope in apiKey.Scope.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
          {
            string[] parsedScope = scope.Split(':');

            if (parsedScope.Length == 2)
            {
              string claimType = parsedScope[0];
              string claimValue = parsedScope[1];

              switch (claimType)
              {
                case "role":
                  // "role" is an abbreviation for claim type http://schemas.microsoft.com/ws/2008/06/identity/claims/role
                  claimType = ClaimTypes.Role;
                  break;
              }

              Logger.LogTrace("Api Key {accessKey}: Adding Claim '{claimtype}' '{claimvalue}'.", accessKey, claimType, claimValue);
              claims.Add(new Claim(claimType, claimValue));
            }
          }

          identity = new ClaimsIdentity(claims, Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME);
          principal = new ClaimsPrincipal(identity);

          return AuthenticateResult.Success(new AuthenticationTicket(principal, Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME));
        }
      }
    }

    private Task<AuthenticateResult> HandleUnauthenticatedRequest()
    {
      ClaimsIdentity identity;
      ClaimsPrincipal principal;

      identity = BuildUnAuthenticatedIdentity();
      principal = new ClaimsPrincipal(identity);

      return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Nucleus.Abstractions.Authentication.Constants.DEFAULT_AUTH_SCHEME)));
    }

    private ClaimsIdentity BuildUnAuthenticatedIdentity()
    {
      // Anonymous users are authenticated - it is not the role of the authentication handler to deny access to anonymous users, 
      // since anonymous users can still access functionality that is allowed for "all users", we just identify them as anonymous.
      Logger.LogTrace("Anonymous user.  Adding Claim {claimType}.", ClaimTypes.Anonymous);

      // This overload creates an identity with IsAuthenticated=false, which is what we want for unauthenticated users
      return new ClaimsIdentity(new List<Claim> { { new Claim(ClaimTypes.Anonymous, "") } });
    }

    private static Boolean IsEqual(System.Net.IPAddress address1, System.Net.IPAddress address2)
    {
      if (System.Net.IPAddress.IsLoopback(address1) && System.Net.IPAddress.IsLoopback(address2)) return true;
      return address1.MapToIPv6().GetAddressBytes().SequenceEqual(address2.MapToIPv6().GetAddressBytes());
    }

    /// <summary>
    /// Handle 401 Challenge responses by redirecting to the login page.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
      if (this.Context.Request.Path.StartsWithSegments(new PathString("/admin")))
      {
        // special handling for admin menu to display a message rather than redirect to login
        await this.Context.Response.WriteAsync("Session expired.");
      }
      else if (this.Context.Request.IsSigned(out Guid _))
      {
        // special handing for API key access (invalid key, or disabled key, or invalid signature), return a 403: Forbidden
        this.Context.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
        await this.Context.Response.WriteAsync("{'error': 'Invalid API Key.'}");
      }
      else
      {
        if (this.CurrentContext.Site != null)
        {
          SitePages sitePage = this.Context.RequestServices.GetService<Context>().Site.GetSitePages();
          PageRoute loginPageRoute = null;

          if (sitePage.LoginPageId.HasValue)
          {
            Page loginPage = await this.Context.RequestServices.GetService<IPageManager>().Get(sitePage.LoginPageId.Value);
            if (loginPage != null)
            {
              loginPageRoute = loginPage.DefaultPageRoute();
            }
          }
          if (loginPageRoute == null)
          {
            // Use default login page
            this.RedirectToDefaultLogin();
          }
          else
          {
            string redirectUrl = this.Context.Request.PathBase + loginPageRoute.Path + $"?returnUrl={GetReturnUrl(this.Context)}";// {System.Uri.EscapeDataString(this.Context.Request.Path)}";
            Logger.LogTrace("Challenge: Redirecting to site login page {redirectUrl}", redirectUrl);
            this.Context.Response.Redirect(redirectUrl);
          }
        }
        else
        {
          // use default login page
          this.RedirectToDefaultLogin();
        }
      }
    }

    private static string GetReturnUrl(HttpContext context)
    {
      return System.Uri.EscapeDataString(context.Request.PathBase + context.Request.Path);
    }

    /// <summary>
    /// Redirect to the default "built in" login page
    /// </summary>
    private void RedirectToDefaultLogin()
    {
      RouteValueDictionary routeDictionary = new()
      {
        { "area", "User" },
        { "controller", "Account" },
        { "action", "Index" }
      };

      string redirectUrl = this.LinkGenerator.GetPathByRouteValues("Admin", routeDictionary, this.Context.Request.PathBase, FragmentString.Empty, null);
      Logger.LogTrace("Challenge: Redirecting to default login page {redirectUrl}", redirectUrl);
      this.Context.Response.Redirect(redirectUrl);
    }

    /// <summary>
    /// Create and add the session cookie to the response.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="properties"></param>
    private void AppendCookie(string sessionId, AuthenticationProperties properties)
    {
      // SameSite = Lax is important in this context.  
      // - For OAUTH2, if a user logs in to a remote authentication site & is redirected back (and is then logged in and redirected to
      //   the home page), browsers treat the entire sequence of requests as "originating" from the remote authentication site, thus
      //   if SameSite=Strict, the session ID is not sent by the browser when it follows the redirect.  SameSite=Lax allows the cookie to 
      //   be sent even though the sequence of requests/responses "originates" at the remote authentication site.
      // - For any other case where a user clicks a link on another site which links to a Nucleus site, and is already logged in to the
      //   Nucleus site, SameSite=Strict would prevent the browser from sending the session cookie (but if SameSite=Lax it will work).
      CookieOptions options = new()
      {
        Expires = properties.IsPersistent ? properties.ExpiresUtc : null,
        IsEssential = true,
        HttpOnly = true,
        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
        Secure = this.Context.Request.IsHttps         
      };

      this.Context.Response.Cookies.Append(this.Options.CookieName, sessionId, options);
    }

    /// <summary>
    /// Handle sign in by creating/adding the session cookie to the response.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
    {
      Logger.LogTrace("Logging in '{name}'.", user.Identity.Name);
      AppendCookie(user.FindFirstValue(Nucleus.Abstractions.Authentication.Constants.SESSION_ID_CLAIMTYPE), properties);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Handle sign out by deleting the session from the database and removing the session cookie from the response.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    protected override async Task HandleSignOutAsync(AuthenticationProperties properties)
    {
      string sessionId = this.Context.Request.Cookies[this.Options.CookieName];

      if (!String.IsNullOrEmpty(sessionId))
      {
        Logger.LogTrace("Logging out: Removing session '{sessionId}'.", sessionId);

        UserSession userSession = await this.SessionManager.Get(Guid.Parse(sessionId));

        if (userSession != null)
        {
          await this.SessionManager.Delete(userSession);
        }
      }

      this.Context.Response.Cookies.Delete(this.Options.CookieName);
    }
  }
}
