using Nucleus.Abstractions.Models.Configuration;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Core.Logging;
using Microsoft.AspNetCore.Authentication.Negotiate;
using System.Threading;

namespace Nucleus.Core.Authentication;

public class ExternalAuthenticationHandler
{
  private ISessionManager SessionManager { get; }
  private IUserManager UserManager { get; }
  private IRoleManager RoleManager { get; }
  private Context NucleusContext { get; }
  private IOptions<AuthenticationProtocols> AuthenticationProtocols { get; }
  private ILogger<ExternalAuthenticationHandler> Logger { get; }

  public ExternalAuthenticationHandler(ISessionManager sessionManager, IUserManager userManager, IRoleManager roleManager, Context nucleusContext, IOptions<AuthenticationProtocols> options, ILogger<ExternalAuthenticationHandler> logger)
  {
    this.SessionManager = sessionManager;
    this.UserManager = userManager;
    this.RoleManager = roleManager;
    this.NucleusContext = nucleusContext;
    this.AuthenticationProtocols = options;
    this.Logger = logger;
  }

  /// <summary>
  /// Copy data from the authentication source, create a user if required & configured and create a Nucleus session for the user.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="context"></param>
  /// <returns></returns>
  /// <remarks>
  /// Use this method to log a user in after receiving a successful response from an external authentication provider.
  /// Redirecting the response after authentication is successful (if required) is up to the controller action which triggers authentication.
  /// </remarks>
  public async Task HandleExternalAuthentication<T>(ResultContext<T> context, Boolean handleLdapClaims)
  where T : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
  {
    this.Logger?.LogTrace("HandleExternalAuthentication: Identity: {identity}.", context.Principal?.Identity?.Name);

    if (context.Principal.Identity.IsAuthenticated)
    {
      AuthenticationProtocol protocolOptions = this.AuthenticationProtocols.Value.Where(protocol => protocol.Scheme == context.Scheme.Name).FirstOrDefault();

      if (handleLdapClaims)
      {
        // handle Windows (Negotiate) authentication.  Call HandleLdapClaims to retrieve/sync data with Ldap 
        if (context.Options is NegotiateOptions options)
        {
          LdapContext ldapContext = new(context.HttpContext, context.Scheme, options as NegotiateOptions, new()
          {
            Domain = ExternalAuthenticationHandler.ResolveDomain(protocolOptions, this.Logger),
            LdapConnection = null,             // HandleLdapClaims creates a new LdapConnection
            EnableLdapClaimResolution = false  // prevent the Microsoft implementation from doing Claims resolution
          })
          {
            Principal = context.Principal
          };

          await HandleLdapClaims(ldapContext);

          // copy the result
          if (ldapContext.Result != null)
          {
            if (ldapContext.Result.Succeeded)
            {
              context.Success();
            }
            else if (ldapContext.Result.Failure != null)
            {
              context.Fail(ldapContext.Result.Failure);
            }
          }
          else
          {
            context.NoResult();
          }
        }
        else
        {
          // if context.Options is not a NegotiateOptions, there's a misconfiguration of some kind.  This should never happen
          throw new InvalidOperationException("Unable to retrieve Ldap claims (options is not of type NegotiateOptions).");
        }
      }
      else
      {
        // If we add support for other external authentication methods in the future and/or update the SAML and OAUTH extensions to call this function
        // instead of handling Nucleus signin themselves, then this code path could be used.
        // When using Windows Authentication, we won't end up here, because:
        // (a) we are not enabling built-in Ldap handling by the Microsoft Negotiate implementation 
        // (b) even if we did, the Microsoft Negotiate implementation does not raise the OnAuthenticated event after a return from the 
        //     OnRetrieveLdapClaims event.
        await CreateNucleusSession(context, protocolOptions);
      }
      
    }
  }

  /// <summary>
  /// Check for/create or sync Nucleus user, and create a Nucleus session.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="context"></param>
  /// <param name="protocolOptions"></param>
  /// <returns></returns>
  private async Task CreateNucleusSession<T>(ResultContext<T> context, AuthenticationProtocol protocolOptions)
    where T : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
  {
    // remove the domain name from the user name, if protocolOptions.IgnoreDomainName is set to true
    string userName = context.Principal.Identity.Name;

    if (protocolOptions.UserRemoveDomainName)
    {
      if (userName.Contains('\\'))
      {
        // in Windows, the user principal name is in the form DOMAIN\username
        userName = userName[(userName.IndexOf('\\') + 1)..];
      }
      else if (userName.Contains('@'))
      {
        // in Linux, the user principal name is generally in the form username@domain
        userName = userName[..userName.IndexOf('@')];
      }
    }

    this.Logger?.LogTrace("Checking username: {name}.", userName);

    // look for an existing user
    User loginUser = await this.UserManager.Get(this.NucleusContext.Site, userName);

    if (loginUser != null && loginUser.IsSiteAdmin(this.NucleusContext.Site) && !protocolOptions.AllowedUsers.HasFlag(AuthenticationProtocol.UserTypes.SiteAdmins))
    {
      this.Logger?.LogError("User {user} was authenticated using '{Scheme}' authentication, but configuration does not allow site admins to log in using '{Scheme}' authentication.", userName, protocolOptions.Scheme, protocolOptions.Scheme);
      context.Fail("Access Denied.");
      return;
    }

    if (loginUser == null)
    {
      // try system admin
      loginUser = await this.UserManager.GetSystemAdministrator(userName);

      if (loginUser != null && loginUser.IsSystemAdministrator() && !protocolOptions.AllowedUsers.HasFlag(AuthenticationProtocol.UserTypes.SystemAdmins))
      {
        this.Logger?.LogError("A system administrator user '{user}' was authenticated using '{Scheme}' authentication, but configuration does not allow system admins to log in using '{Scheme}' authentication.", userName, protocolOptions.Scheme, protocolOptions.Scheme);
        context.Fail("Access Denied.");
        return;
      }
    }

    if (loginUser == null)
    {
      // create a new user (if configured)
      if (protocolOptions.CreateUsers)
      {
        this.Logger?.LogDebug("User: {name} not found, creating.", userName);

        // create new user 
        loginUser = await this.UserManager.CreateNew(this.NucleusContext.Site);
        loginUser.UserName = userName;
        this.UserManager.SetNewUserFlags(this.NucleusContext.Site, loginUser);

        loginUser.Verified = true;
        loginUser.Approved = true;

        await SyncUser(protocolOptions, context.Principal, this.NucleusContext.Site, await this.RoleManager.List(this.NucleusContext.Site), loginUser);
        await this.UserManager.Save(this.NucleusContext.Site, loginUser);
      }
      else
      {
        this.Logger.LogWarning("User {user} was authenticated using '{Scheme}' authentication, but the user does not exist in Nucleus and the protocol settings do not allow user creation.", userName, protocolOptions.Scheme);
        context.Fail("Access Denied.");
        return;
      }
    }
    else
    {
      // sync data, depending on config settings, unless the user is a system administrator
      if (protocolOptions.UserSyncOptions != AuthenticationProtocol.SyncOptions.None && !loginUser.IsSystemAdministrator())
      {
        await SyncUser(protocolOptions, context.Principal, this.NucleusContext.Site, await this.RoleManager.List(this.NucleusContext.Site), loginUser);
        await this.UserManager.Save(this.NucleusContext.Site, loginUser);
      }
    }

    // create a Nucleus session
    if (loginUser != null)
    {
      this.Logger?.LogDebug("Creating session for user: '{name}'.", loginUser.UserName);

      UserSession session = await this.SessionManager.CreateNew(this.NucleusContext.Site, loginUser, false, context.Request.HttpContext.Connection.RemoteIpAddress);
      await this.SessionManager.SignIn(session, context.Request.HttpContext, "");      
    }
    else
    {
      this.Logger?.LogDebug("No user found for user name: '{name}'.", userName);
      context.Fail($"User name '{userName}' is incorrect.");
    }
  }

  /// <summary>
  /// Read claims from Ldap and update context.Principal, so that HandleExternalAuthentication has claims data to sync the user.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="context"></param>
  /// <returns></returns>
  public async Task HandleLdapClaims(LdapContext context)
  {
    this.Logger?.LogTrace("HandleLdapClaims: Identity: {identity}.", context.Principal?.Identity?.Name);

    if (context.Principal.Identity.IsAuthenticated)
    {
      AuthenticationProtocol protocolOptions = this.AuthenticationProtocols.Value.Where(protocol => protocol.Scheme == context.Scheme.Name).FirstOrDefault();

      if (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Profile) || (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Roles)))
      {
        LdapConnection connection = context.LdapSettings.LdapConnection;

        if (connection == null)
        {
          // if the Ldap connection was not successfully created during startup, try to create one now
          try
          {
            connection = BuildLdapConnection(protocolOptions, false, this.Logger);
            context.LdapSettings.LdapConnection = connection;
          }
          catch (OperationCanceledException ex)
          {
            this.Logger?.LogError(ex, "The LdapConnection bind operation for scheme '{scheme}' '{domain}' timed out.", protocolOptions.Scheme, protocolOptions.LdapDomain);
            connection = null;
          }
          catch (LdapException ex)
          {
            this.Logger?.LogError(ex, "Error building LdapConnection for scheme '{scheme}' '{domain}'.  Failed to connect to LDAP [{code}]. ", protocolOptions.Scheme, protocolOptions.LdapDomain, ex.ErrorCode);
            connection = null;
          }
          catch (Exception ex)
          {
            this.Logger?.LogError(ex, "Error building LdapConnection for scheme '{scheme}' '{domain}'", protocolOptions.Scheme, protocolOptions.LdapDomain);
            connection = null;
          }
        }

        if (connection != null)
        {
          // Ensure that connection settings can be created by the initial call to BuildLdapConnection (from AuthenticationExtensions.AddCoreAuthentication), but if 
          // that fails or times out, we initialize the Negotiate protocol with a null connection and the Microsoft implementation can create the
          // connection.  If that happens, we need to set some connection options because the Microsoft defaults to referral chasing=all, which can
          // make Ldap searches very slow.  ProtocolVersion = 3 is important for Linux.
          connection.SessionOptions.ProtocolVersion = 3;
          connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;

          string userSid = context.Principal.Claims
            .Where(claim => claim.Type == ClaimTypes.PrimarySid)
            .Select(claim => claim.Value)
            .FirstOrDefault();

          List<Claim> userLdapClaims = GetLdapUserProperties(connection, userSid, this.Logger);

          ClaimsIdentity identity = new(userLdapClaims, context.Scheme.Name);
          context.Principal = new(identity);
          context.Success();

          // The code in the Negotiate handler doesn't raise the OnAuthenticated when we set a Success status from HandleLdapClaims
          // so we have to call it ourselves
          await CreateNucleusSession(context, protocolOptions);
          //await HandleExternalAuthentication(context);
        }
        else
        {
          this.Logger?.LogWarning("HandleLdapClaims: Identity: {identity}.  LdapConnection is null, no query was sent.", context.Principal?.Identity?.Name);
          context.Fail("LdapConnection is null.");
        }
      }
    }
  }

  /// <summary>
  /// Copy profile properties and roles, depending on the value of protocolOptions.UserSyncOptions.
  /// </summary>
  /// <param name="protocolOptions"></param>
  /// <param name="principal"></param>
  /// <param name="site"></param>
  /// <param name="roles"></param>
  /// <param name="loginUser"></param>
  /// <param name="logger"></param>
  /// <returns></returns>
  private Task<User> SyncUser(AuthenticationProtocol protocolOptions, ClaimsPrincipal principal, Site site, IEnumerable<Role> roles, User loginUser)
  {
    if (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Profile))
    {
      this.Logger?.LogDebug("Updating user profile properties for user: '{name}'.", loginUser.UserName);
      // fill in all of the user properties that we can find a value for
      foreach (UserProfileProperty prop in site.UserProfileProperties)
      {
        string userPropertyValue = principal.Claims
          .Where(claim => claim.Type == prop.TypeUri)
          .Select(claim => claim.Value)
          .FirstOrDefault();

        if (userPropertyValue != null)
        {
          this.Logger?.LogTrace("Setting profile value '{name}': '{value}'.", prop.TypeUri, userPropertyValue);
          UserProfileValue existing = loginUser.Profile.Where(value => value.UserProfileProperty.TypeUri == prop.TypeUri).FirstOrDefault();
          if (existing == null)
          {
            loginUser.Profile.Add(new UserProfileValue() { UserProfileProperty = prop, Value = userPropertyValue });
          }
          else
          {
            existing.Value = userPropertyValue;
          }
        }
      }
    }

    if (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Roles))
    {      
      List<Claim> roleClaims = principal.Claims
        .Where(claim => claim.Type == ClaimTypes.Role)
        .ToList();

      // only sync roles if the authentication protocol has provided any
      if (roleClaims.Any())
      {
        this.Logger?.LogDebug("Updating user roles for user: '{name}'.", loginUser.UserName);

        foreach (Role roleToRemove in loginUser.Roles
          .Where(role => CanRemoveRole(site, role) && !roleClaims.Where(claim => claim.Value.Equals(role.Name, StringComparison.OrdinalIgnoreCase)).Any()).ToList())
        {
          this.Logger?.LogTrace("Removing user '{user}' from role '{roleToRemove}'.", loginUser.UserName, roleToRemove.Name);
          loginUser.Roles.Remove(roleToRemove);
        }

        // add roles that are in the LDAP response but are not in the user roles list
        foreach (Claim ldapRole in roleClaims
          .Where(ldapRole => !loginUser.Roles.Where(role => CanAddRole(site, role) && role.Name.Equals(ldapRole.Value, StringComparison.OrdinalIgnoreCase)).Any()).ToList())
        {
          Role newRole = roles
            .Where(role => role.Name.Equals(ldapRole.Value, StringComparison.OrdinalIgnoreCase) && role.Id != site.RegisteredUsersRole.Id)
            .FirstOrDefault();
          if (newRole != null)
          {
            this.Logger?.LogTrace("Adding user '{user}' to role '{roleToRemove}'.", loginUser.UserName, newRole.Name);
            loginUser.Roles.Add(newRole);
          }
        }
      }
      else
      {
        this.Logger?.LogDebug("Not updating user roles for user: '{name}' because the '{scheme}' authentication provider did not return any roles.", loginUser.UserName, protocolOptions.Scheme);
      }
    }

    this.Logger?.LogDebug("Sync complete for user '{user}'.", loginUser.UserName);

    return Task.FromResult(loginUser);
  }

  /// <summary>
  /// Map well-known LDAP property names to claim type urn.  If the LDAP attribute name is not well-known, return null.
  /// </summary>
  /// <param name="ldapAttributeName"></param>
  /// <returns></returns>
  private static string MapLdapAttributeNameToClaimType(string ldapAttributeName)
  {
    switch (ldapAttributeName.ToLower())
    {
      case "userprincipalname":
        return ClaimTypes.Name;

      case "wwwhomepage":
      case "webpage":
        return ClaimTypes.Webpage;

      case "givenname":
        return ClaimTypes.GivenName;

      case "sn":
        return ClaimTypes.Surname;

      case "mail":
        return ClaimTypes.Email;

      case "telephonenumber":
        return ClaimTypes.OtherPhone;

      case "mobile":
        return ClaimTypes.MobilePhone;

      case "streetaddress":
        return ClaimTypes.StreetAddress;

      case "locality":
        return ClaimTypes.Locality;

      case "st":
        return ClaimTypes.StateOrProvince;

      case "co":
      case "country":
        return ClaimTypes.Country;

      case "postalcode":
        return ClaimTypes.PostalCode;

      case "memberof":
        return ClaimTypes.Role;
    }

    return null;
  }

  private static Boolean CanAddRole(Site site, Role role)
  {
    // auto-assigned roles and "registered users" role are handled automatically by this.UserManager.CreateNew for new users, and should not
    // be modified by synchronization with ldap
    return
      !role.Type.HasFlag(Role.RoleType.Restricted) && !role.Type.HasFlag(Role.RoleType.AutoAssign) &&
      role.Id != site.AdministratorsRole.Id &&
      role.Id != site.AllUsersRole.Id &&
      role.Id != site.AnonymousUsersRole.Id &&
      role.Id != site.RegisteredUsersRole.Id;
  }

  private static Boolean CanRemoveRole(Site site, Role role)
  {
    return
      !role.Type.HasFlag(Role.RoleType.AutoAssign) && !role.Type.HasFlag(Role.RoleType.Restricted) &&
      role.Id != site.AdministratorsRole.Id &&
      role.Id != site.AllUsersRole.Id &&
      role.Id != site.AnonymousUsersRole.Id &&
      role.Id != site.RegisteredUsersRole.Id;
  }

  public static string ResolveDomain(AuthenticationProtocol protocolOptions, ILogger logger)
  {
    string domain = protocolOptions.LdapDomain;

    if (domain.Equals("auto", StringComparison.OrdinalIgnoreCase))
    {
      if (OperatingSystem.IsLinux())
      {
        if (!System.IO.File.Exists("/etc/krb5.keytab"))
        {
          // no keytab file = not joined to a domain, fail
          logger?.LogInformation("keytab file (/etc/krb5.keytab) is not present or is not accessible, cannot enable LDAP for Kerberos.");
          return null;
        }

        // System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName does not work in Linux
        // look in /etc/krb5.conf for the line: default_realm = [domain]
        if (System.IO.File.Exists("/etc/krb5.conf"))
        {
          string data = System.IO.File.ReadAllText("/etc/krb5.conf");
          System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(data, ".*default_realm[\\s]*=[\\s]*(?<domain>.*)");
          if (match.Success && match.Groups.ContainsKey("domain"))
          {
            domain = match.Groups["domain"].Value.ToLower();
          }
        }
        else
        {
          logger?.LogInformation("Kerberos configuration file (/etc/krb5.conf) is not present or is not accessible, cannot retrieve default LDAP domain to enable LDAP for Kerberos.");
          return null;
        }
      }
      else
      {
        domain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
      }
    }

    return domain;
  }

  public static LdapConnection BuildLdapConnection(AuthenticationProtocol protocolOptions, Boolean bindAsync, ILogger logger)
  {
    LdapConnection connection;

    string domain = ResolveDomain(protocolOptions, logger);

    logger?.LogInformation("Using LDAP domain '{domain}'.", domain);

    LdapDirectoryIdentifier endPoint = new(domain, false, false);

    if (endPoint.Servers != null)
    {
      logger?.LogTrace("Creating an LDAP connection to '{domain}/{port}'.", String.Join(',', endPoint.Servers), endPoint.PortNumber);
    }
    else
    {
      logger?.LogTrace("Creating an LDAP connection to 'null/{port}'.", endPoint.PortNumber);
    }

    connection = new(endPoint);
    if (!String.IsNullOrEmpty(protocolOptions.LdapMachineAccountName))
    {
      // if specified in config, username should include the user and the user's domain.  This is so that we can be flexible about what is in protocolOptions.LdapDomain - it
      // can be either just a domain name, or it can be a the fully-qualified name of the Ldap server.
      connection.Credential = new System.Net.NetworkCredential(protocolOptions.LdapMachineAccountName, protocolOptions.LdapMachineAccountPassword);
      connection.AuthType = AuthType.Basic;
      logger?.LogTrace("Using credentials from config for user '{user}' for the new LDAP connection to '{domain}'.", protocolOptions.LdapMachineAccountName, String.Join(',', endPoint.Servers));
    }
    else
    {
      logger?.LogTrace("Using Kerberos (Negotiate) authentication for the LDAP connection to '{domain}'.", String.Join(',', endPoint.Servers));
      connection.AuthType = AuthType.Negotiate;
    }

    connection.SessionOptions.ProtocolVersion = 3;
    connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;

    // this causes an "A local error occurred" error in Linux
    // connection.Timeout = TimeSpan.FromSeconds(15);  


    logger?.LogTrace("Binding LDAP connection.");

    CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(15));

    // connection.Timeout does not work because of a bug, so we have to implement our own timeout.
    Task task = Task.Run(() =>
    {
      connection.Bind();
      logger?.LogTrace("Ldap Bind complete.");
    });

    if (!bindAsync)
    {
      task.Wait(cancellationTokenSource.Token);
    }

    return connection;
  }

  private static List<Claim> GetLdapUserProperties(LdapConnection connection, string userSid, ILogger logger)
  {
    List<Claim> results = new();

    string dn = String.Join(',',
      ((LdapDirectoryIdentifier)connection.Directory).Servers
        .FirstOrDefault()
        ?.Split('.', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => $"DC={part}"));

    string ldapFilter = $"(objectSid={userSid})";

    SearchRequest request = new() { DistinguishedName = dn, Filter = ldapFilter, Scope = SearchScope.Subtree };
    SearchResponse response = (SearchResponse)connection.SendRequest(request);

    if (response.ResultCode == ResultCode.Success)
    {
      if (response.Entries.Count > 0)
      {
        SearchResultEntry entry = response.Entries[0];

        foreach (string attributeName in entry.Attributes.AttributeNames)
        {
          string claimType = MapLdapAttributeNameToClaimType(attributeName);

          if (claimType != null)
          {
            if (claimType == ClaimTypes.Role)
            {
              // special parsing is required for roles
              foreach (string memberOf in entry.Attributes[attributeName].GetValues(typeof(string)).Cast<string>())
              {
                string roleName = ParseRoleName(memberOf);

                if (roleName != null)
                {
                  results.Add(new(claimType, roleName));
                }
              }
            }
            else
            {
              foreach (string value in entry.Attributes[attributeName].GetValues(typeof(string)).Cast<string>())
              {
                results.Add(new(claimType, value));
              }
            }
          }
        }
      }
    }
    else
    {
      logger?.LogWarning("Ldap search for dn:'{dn}', filter: '{filter}' failed with error ({code}).", dn, ldapFilter, response.ResultCode);
    }

    return results;
  }

  /// <summary>
  /// Returns a role name from a string returned in the memberOf property of an Ldap response.
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  /// <remarks>
  /// For example, return "Inventua Developers" from CN=Inventua Developers,OU=Inventua Users and Roles,DC=inventua,DC=com
  /// </remarks>
  private static string ParseRoleName(string value)
  {
    if (value != null)
    {
      System.Text.RegularExpressions.Match roleNameMatch = System.Text.RegularExpressions.Regex.Match(value, "^CN=(?<rolename>[^,]*)");

      if (roleNameMatch.Success)
      {
        return roleNameMatch.Groups["rolename"].Value;
      }
    }

    return null;
  }

  //private static IEnumerable<string> GetLdapRoles(LdapConnection connection, IEnumerable<string> sidList)
  //{
  //  List<string> results = new();
  //  System.Text.StringBuilder ldapFilter = new();

  //  //ldapFilter.Append("(&(objectType=group)");

  //  ldapFilter.Append("(|");
  //  foreach (string sid in sidList)
  //  {
  //    ldapFilter.Append("(objectSid=");
  //    ldapFilter.Append(sid);
  //    ldapFilter.Append(")");
  //  }
  //  ldapFilter.Append(")");

  //  // ldapFilter.Append(")");

  //  SearchRequest request = new() { DistinguishedName = "DC=inventua,DC=com", Filter = ldapFilter.ToString(), Scope = SearchScope.Subtree };
  //  SearchResponse response = (SearchResponse)connection.SendRequest(request);

  //  if (response.ResultCode == ResultCode.Success)
  //  {
  //    for (int count = 0; count < response.Entries.Count; count++)
  //    {
  //      string roleName = (string)response.Entries[count].Attributes["name"].GetValues(typeof(string)).FirstOrDefault();
  //      if (roleName != null)
  //      {
  //        results.Add(roleName);
  //      }
  //    }
  //  }

  //  return results;
  //}
}
