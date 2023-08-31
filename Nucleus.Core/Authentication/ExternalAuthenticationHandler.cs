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
    this.SessionManager=sessionManager;
    this.UserManager = userManager;
    this.RoleManager = roleManager;
    this.NucleusContext= nucleusContext;
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
          BuildLdapConnection(authenticationProtocol);
        }
        catch (Exception ex)
        {
          services.Logger()?.LogError(ex, "Error building LdapConnection for scheme '{scheme}' '{domain}'", authenticationProtocol.Scheme, authenticationProtocol.Domain);
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
    if (context.Principal.Identity.IsAuthenticated)
    {      
      AuthenticationProtocol protocolOptions = this.AuthenticationProtocols.Value.Where(protocol => protocol.Scheme == context.Scheme.Name).FirstOrDefault();

      // remove the domain name from the user name, if protocolOptions.IgnoreDomainName is set to true
      string userName = context.Principal.Identity.Name;
      if (protocolOptions.UserRemoveDomainName && userName.Contains('\\'))
      {
        userName = userName.Substring(userName.IndexOf('\\') + 1);
      }

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
        UserSession session = await this.SessionManager.CreateNew(this.NucleusContext.Site, loginUser, false, context.Request.HttpContext.Connection.RemoteIpAddress);
        await this.SessionManager.SignIn(session, context.Request.HttpContext, "");

        // redirecting the response (if required) is up to the controller action which triggers Negotiate/Windows authentication.  
        // This is normally Nucleus.Modules.Account.Controllers.LoginController.Negotiate(), but other controller actions can trigger
        // Windows authentication by having a [Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)] attribute.
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
          this.Logger?.LogTrace("Adding profile value {name}:'{value}'.", prop.TypeUri, userPropertyValue);
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

    LdapConnection connection = BuildLdapConnection(protocolOptions);

    if (protocolOptions.UserSyncOptions.HasFlag(AuthenticationProtocol.SyncOptions.Profile))
    {
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
      // handle Active Directory roles (get role names from LDAP).  Kerberos only populates role SIDs.
      IEnumerable<string> sidList = principal.Claims
        .Where(claim => claim.Type == ClaimTypes.GroupSid && !IGNORED_SIDS.Contains(claim.Value))
        .Select(claim => claim.Value);

      IEnumerable<string> ldapRoles = GetLdapRoles(connection, sidList);

      //if (ldapRoles.Any())
      //{
        // remove roles that aren't in the LDAP response, except for the registered users and site administrators roles
        foreach (Role roleToRemove in loginUser.Roles
          .Where(role => CanRemoveRole(site, role) && !ldapRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase)).ToList())
        {
          loginUser.Roles.Remove(roleToRemove);
        }

        // add roles that are in the LDAP response but are not in the user roles list
        foreach (string ldapRole in ldapRoles
          .Where(ldapRole => !loginUser.Roles
            .Where(role => CanAddRole(role) && role.Name.Equals(ldapRole, StringComparison.OrdinalIgnoreCase))
            .Any()))
        {
          Role newRole = roles
            .Where(role => role.Name.Equals(ldapRole, StringComparison.OrdinalIgnoreCase) && role.Id != site.RegisteredUsersRole.Id)
            .FirstOrDefault();
          if (newRole != null)
          {
            loginUser.Roles.Add(newRole);
          }
        }
      //}
    }

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

  private static Boolean CanAddRole(Role role)
  {
    return !role.Type.HasFlag(Role.RoleType.Restricted);
  }

  private static Boolean CanRemoveRole(Site site, Role role)
  {
    return
      role.Id != site.RegisteredUsersRole.Id &&
      !role.Type.HasFlag(Role.RoleType.AutoAssign) &&
      !role.Type.HasFlag(Role.RoleType.Restricted) &&
      role.Id != site.AdministratorsRole.Id;
  }

  private static LdapConnection BuildLdapConnection(AuthenticationProtocol protocolOptions)
  {
    LdapConnection connection = GetConnection(protocolOptions.Scheme);

    if (connection == null)
    {
      // protocolOptions.Domain can be null (which works OK with the LdapDirectoryIdentifier constructor).  This would only work if the service user
      // has permission to query LDAP or LDAP can be queried anonymously.
      LdapDirectoryIdentifier endPoint = new(protocolOptions.Domain, false, false);

      connection = new(endPoint);
      if (!String.IsNullOrEmpty(protocolOptions.MachineAccountName))
      {
        // if specified, username should be in the form domain\username
        connection.Credential = new System.Net.NetworkCredential($"{protocolOptions.MachineAccountName}@{protocolOptions.Domain}", protocolOptions.MachineAccountPassword);
      }

      connection.SessionOptions.ProtocolVersion = 3;
      connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
      connection.Timeout = TimeSpan.FromSeconds(10);

      connection.Bind();

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
