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
using Microsoft.AspNetCore.Authentication.Negotiate;
using Nucleus.Core.Logging;

namespace Nucleus.Core.Authentication;

public class ExternalAuthenticationHandler
{
  private static System.Collections.Concurrent.ConcurrentDictionary<string, LdapConnection> LdapConnections { get; } = new();

  private static string[] IGNORED_SIDS = { "S-1-0-0", "S-1-1-0", "S-1-2-0", "S-1-3-0", "S-1-3-1", "S-1-5-4", "S-1-5-11" };

  private ISessionManager SessionManager { get; }
  private IUserManager UserManager { get; }
  private IRoleManager RoleManager { get; }
  private Context NucleusContext { get; }
  private IOptions<AuthenticationProtocols> AuthenticationProtocols { get; }
  private ILogger<AuthenticationHandler> Logger { get; }

  public ExternalAuthenticationHandler(ISessionManager sessionManager, IUserManager userManager, IRoleManager roleManager, Context nucleusContext, IOptions<AuthenticationProtocols> options, ILogger<AuthenticationHandler> logger)
  {
    this.SessionManager = sessionManager;
    this.UserManager = userManager;
    this.RoleManager = roleManager;
    this.NucleusContext = nucleusContext;
    this.AuthenticationProtocols = options;
    this.Logger = logger;
  }

  /// <summary>
  /// Configure and bind LDAP protocols.
  /// </summary>
  /// <typeparam name=""></typeparam>
  /// <param name="authenticationProtocols"></param>
  /// <returns></returns>
  /// <remarks>
  /// This function creates and binds LDAP protocols and is intended for during configuration.  LDAP connections take a few seconds to 
  /// connect - calling this function means that they are ready when they are needed.
  /// </remarks>
  internal static void InitializeLDAP(IServiceCollection services, AuthenticationProtocols authenticationProtocols)
  {
    if (authenticationProtocols != null)
    {
      foreach (AuthenticationProtocol authenticationProtocol in authenticationProtocols)
      {
        try
        {
          BuildLdapConnection(authenticationProtocol, services.Logger());
        }
        catch (Exception ex)
        {
          services.Logger()?.LogError(ex, "Error building LdapConnection for scheme '{scheme}' '{domain}'", authenticationProtocol.Scheme, authenticationProtocol.LdapDomain);
        }
      }
    }
  }

  public static LdapConnection GetConnection(string scheme)
  {
    return LdapConnections.ContainsKey(scheme) ? LdapConnections[scheme] : null;
  }

  /// <summary>
  /// Copy data from the authentication source, create a user if required & configured and create a Nucleus session for the user.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="context"></param>
  /// <returns></returns>
  /// <remarks>
  /// Use this method to log a user in after receiving a successful response from an external authentication provider.
  /// </remarks>
  public async Task HandleExternalAuthentication<T>(ResultContext<T> context)
  where T : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
  {
    this.Logger?.LogTrace("HandleExternalAuthentication");

    if (context.Principal.Identity.IsAuthenticated)
    {
      this.Logger?.LogTrace("Identity: {identity}.", context.Principal.Identity?.Name);

      AuthenticationProtocol protocolOptions = this.AuthenticationProtocols.Value.Where(protocol => protocol.Scheme == context.Scheme.Name).FirstOrDefault();

      // remove the domain name from the user name, if protocolOptions.IgnoreDomainName is set to true
      string userName = context.Principal.Identity.Name;
      if (protocolOptions.UserRemoveDomainName)
      {
        if (userName.Contains('\\'))
        { 
          // in Windows, the user principal name is in the form DOMAIN\username
          userName = userName.Substring(userName.IndexOf('\\') + 1);
        }
        else if (userName.Contains('@'))
        {
          // in Linux, the user principal name is generally in the form username@domain
          userName = userName.Substring(0, userName.IndexOf('@'));
        }
      }

      this.Logger?.LogTrace("Checking username: {name}.", userName);

      // look for an existing user
      User loginUser = await this.UserManager.Get(this.NucleusContext.Site, userName);

      if (loginUser != null && loginUser.IsSiteAdmin(this.NucleusContext.Site) && !protocolOptions.AllowedUsers.HasFlag(AuthenticationProtocol.UserTypes.SiteAdmins))
      {
        this.Logger?.LogError("User {user} was authenticated using '{Scheme}' authentication, but configuration does not allow site admins to log in using '{Scheme}' authentication.", userName, protocolOptions.Scheme, protocolOptions.Scheme);
        context.Fail("Access Denied.");
        loginUser = null;
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
          loginUser = null;
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
          this.Logger.LogError("User {user} was authenticated using '{Scheme}' authentication, but the user does not exist in Nucleus and the protocol settings do not allow user creation.", userName, protocolOptions.Scheme);
          context.Fail("Access Denied.");
          loginUser = null;
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

        if (!loginUser.Approved)
        {
          this.Logger?.LogError("User {user} was authenticated using '{Scheme}' authentication, but the Nucleus user account is not approved.", userName, protocolOptions.Scheme);
          context.Fail("Access Denied.");
          loginUser = null;
          return;
        }
        else if (!loginUser.Verified)
        {
          this.Logger?.LogError("User {user} was authenticated using '{Scheme}' authentication, but the Nucleus user account is not verified.", userName, protocolOptions.Scheme);
          context.Fail("Access Denied.");
          loginUser = null;
          return;
        }
      }

      // create a Nucleus session
      if (loginUser != null)
      {
        this.Logger?.LogDebug("Creating session for user: {name}.", loginUser.UserName);

        UserSession session = await this.SessionManager.CreateNew(this.NucleusContext.Site, loginUser, false, context.Request.HttpContext.Connection.RemoteIpAddress);
        await this.SessionManager.SignIn(session, context.Request.HttpContext, "");

        // redirecting the response (if required) is up to the controller action which triggers Negotiate/Windows authentication.  
        // This is normally Nucleus.Modules.Account.Controllers.LoginController.Negotiate(), but other controller actions can trigger
        // Windows authentication by having a [Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)] attribute.
      }
      else
      {
        this.Logger?.LogDebug("No user found for user name: {name}.", userName);
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
    LdapConnection connection = null;

    if (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Profile))
    {
      this.Logger?.LogDebug("Updating user profile properties from original claim for user: '{name}'.", loginUser.UserName);
      // fill in all of the user properties that we can find a value for.  For Authentication.Negotiate, none of the claims returned will match
      // a user profile property, because Authentication.Negotiate doesn't query user attributes, but other (future) external authentication providers
      // might have values that we can use.  
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

    if (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Profile) || protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Roles))
    {
      this.Logger?.LogDebug("Connecting to LDAP.");
      try
      {
        connection = BuildLdapConnection(protocolOptions, this.Logger);
      }
      catch (Exception ex)
      {
        this.Logger?.LogError(ex, "Failed to connect to LDAP.  User data was not synchronized with LDAP.");
        return Task.FromResult(loginUser);
      }
    }

    if (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Profile))
    {
      this.Logger?.LogDebug("Updating user profile properties from LDAP for user: '{name}'.", loginUser.UserName);

      // get user data from ldap
      foreach (KeyValuePair<string, string> attribute in GetLdapUserProperties(connection, principal.Claims
        .Where(claim => claim.Type == ClaimTypes.PrimarySid)
        .Select(claim => claim.Value)
        .FirstOrDefault()))
      {
        string claimType = MapLdapAttributeNameToClaimType(attribute.Key);

        if (claimType != null)
        {
          UserProfileProperty property = site.UserProfileProperties
            .Where(prop => prop.TypeUri.Equals(claimType, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

          if (property != null)
          {
            this.Logger?.LogTrace("Setting profile value '{name}': '{value}'.", property.TypeUri, attribute.Value);
            UserProfileValue existing = loginUser.Profile.Where(value => value.UserProfileProperty.TypeUri == property.TypeUri).FirstOrDefault();
            if (existing == null)
            {
              loginUser.Profile.Add(new UserProfileValue() { UserProfileProperty = property, Value = attribute.Value });
            }
            else
            {
              existing.Value = attribute.Value;
            }
          }
        }
      }
    }

    if (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Roles))
    {
      this.Logger?.LogDebug("Updating user roles from LDAP for user: '{name}'.", loginUser.UserName);

      // handle Active Directory roles (get role names from LDAP).  Kerberos only populates role SIDs.
      IEnumerable<string> sidList = principal.Claims
        .Where(claim => claim.Type == ClaimTypes.GroupSid && !IGNORED_SIDS.Contains(claim.Value))
        .Select(claim => claim.Value);

      IEnumerable<string> ldapRoles = GetLdapRoles(connection, sidList);

      // remove roles that aren't in the LDAP response, except for the registered users and site administrators roles
      foreach (Role roleToRemove in loginUser.Roles
        .Where(role => CanRemoveRole(site, role) && !ldapRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase)).ToList())
      {
        this.Logger?.LogTrace("Removing user '{user}' from role '{roleToRemove}'.", loginUser.UserName, roleToRemove.Name);
        loginUser.Roles.Remove(roleToRemove);
      }

      // add roles that are in the LDAP response but are not in the user roles list
      foreach (string ldapRole in ldapRoles
        .Where(ldapRole => !loginUser.Roles.Where(role => CanAddRole(site, role) && role.Name.Equals(ldapRole, StringComparison.OrdinalIgnoreCase)).Any()))
      {
        Role newRole = roles
          .Where(role => role.Name.Equals(ldapRole, StringComparison.OrdinalIgnoreCase) && role.Id != site.RegisteredUsersRole.Id)
          .FirstOrDefault();
        if (newRole != null)
        {
          this.Logger?.LogTrace("Adding user '{user}' to role '{roleToRemove}'.", loginUser.UserName, newRole.Name);
          loginUser.Roles.Add(newRole);
        }
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

  private static LdapConnection BuildLdapConnection(AuthenticationProtocol protocolOptions, ILogger logger)
  {
    LdapConnection connection = GetConnection(protocolOptions.Scheme);

    if (connection == null)
    {      
      // protocolOptions.Domain can be null (which works OK with the LdapDirectoryIdentifier constructor).  If null, we assume that the underlying
      // implementation (for example, sssd in linux) has been configured to already know which domain to use.
      LdapDirectoryIdentifier endPoint = new(protocolOptions.LdapDomain, false, false);

      logger?.LogTrace("Creating a new LDAP connection to '{domain}'.", String.Join(',', endPoint.Servers));

      connection = new(endPoint);
      if (!String.IsNullOrEmpty(protocolOptions.LdapDomain) && !String.IsNullOrEmpty(protocolOptions.LdapMachineAccountName))
      {
        // if specified in config, username should not include the domain
        connection.Credential = new System.Net.NetworkCredential($"{protocolOptions.LdapMachineAccountName}@{protocolOptions.LdapDomain}", protocolOptions.LdapMachineAccountPassword);
        logger?.LogTrace("Using credentials from config for user '{user}' for the new LDAP connection to '{domain}'.", protocolOptions.LdapMachineAccountName, String.Join(',', endPoint.Servers));
      }

      connection.SessionOptions.ProtocolVersion = 3;
      connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
      connection.Timeout = TimeSpan.FromSeconds(15);

      logger?.LogTrace("Binding LDAP connection.");
      connection.Bind();
      logger?.LogTrace("Bind complete.");

      LdapConnections.TryAdd(protocolOptions.Scheme, connection);
    }

    return connection;
  }

  private static Dictionary<string, string> GetLdapUserProperties(LdapConnection connection, string userSid)
  {
    Dictionary<string, string> results = new(StringComparer.OrdinalIgnoreCase);
    System.Text.StringBuilder ldapFilter = new();

    ldapFilter.Append("(objectSid=");
    ldapFilter.Append(userSid);
    ldapFilter.Append(")");

    SearchRequest request = new() { DistinguishedName = "DC=inventua,DC=com", Filter = ldapFilter.ToString(), Scope = SearchScope.Subtree };
    SearchResponse response = (SearchResponse)connection.SendRequest(request);

    if (response.ResultCode == ResultCode.Success)
    {
      if (response.Entries.Count > 0)
      {
        SearchResultEntry entry = response.Entries[0];

        foreach (string attributeName in entry.Attributes.AttributeNames)
        {
          string attributeValue = (string)entry.Attributes[attributeName].GetValues(typeof(string)).FirstOrDefault();
          results.Add(attributeName, attributeValue);
        }
      }
    }

    return results;
  }

  private static IEnumerable<string> GetLdapRoles(LdapConnection connection, IEnumerable<string> sidList)
  {
    List<string> results = new();
    System.Text.StringBuilder ldapFilter = new();

    //ldapFilter.Append("(&(objectType=group)");

    ldapFilter.Append("(|");
    foreach (string sid in sidList)
    {
      ldapFilter.Append("(objectSid=");
      ldapFilter.Append(sid);
      ldapFilter.Append(")");
    }
    ldapFilter.Append(")");

    // ldapFilter.Append(")");

    SearchRequest request = new() { DistinguishedName = "DC=inventua,DC=com", Filter = ldapFilter.ToString(), Scope = SearchScope.Subtree };
    SearchResponse response = (SearchResponse)connection.SendRequest(request);

    if (response.ResultCode == ResultCode.Success)
    {
      for (int count = 0; count < response.Entries.Count; count++)
      {
        string roleName = (string)response.Entries[count].Attributes["name"].GetValues(typeof(string)).FirstOrDefault();
        if (roleName != null)
        {
          results.Add(roleName);
        }
      }
    }

    return results;
  }
}
