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
using System.Net;

namespace Nucleus.Core.Authentication;

public class ExternalAuthenticationHandler
{  
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
    this.Logger?.LogTrace("HandleExternalAuthentication: Identity: {identity}.", context.Principal?.Identity?.Name);

    if (context.Principal.Identity.IsAuthenticated)
    {
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

        if (connection != null)
        {
          List<Claim> userLdapClaims = GetLdapUserProperties(connection, context.Principal.Claims
            .Where(claim => claim.Type == ClaimTypes.PrimarySid)
            .Select(claim => claim.Value)
            .FirstOrDefault());

          ClaimsIdentity identity = new(userLdapClaims, context.Scheme.Name);
          context.Principal = new(identity);
          context.Success();

          // The code in the Negotiate handler doesn't raise the OnAuthenticated when we set a Success status from HandleLdapClaims
          // so we have to call it ourselves
          await HandleExternalAuthentication(context);
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
      this.Logger?.LogDebug("Updating user roles for user: '{name}'.", loginUser.UserName);
      List<Claim> roleClaims = principal.Claims
        .Where(claim => claim.Type == (principal.Identity as ClaimsIdentity)?.RoleClaimType || claim.Type == ClaimTypes.Role)
        .ToList();

      if (roleClaims.Any())
      {
        foreach (Role roleToRemove in loginUser.Roles
          .Where(role => CanRemoveRole(site, role) && !roleClaims.Where(claim => claim.Value.Equals(role.Name, StringComparison.OrdinalIgnoreCase)).Any()))
        {
          this.Logger?.LogTrace("Removing user '{user}' from role '{roleToRemove}'.", loginUser.UserName, roleToRemove.Name);
          loginUser.Roles.Remove(roleToRemove);
        }

        // add roles that are in the LDAP response but are not in the user roles list
        foreach (Claim ldapRole in roleClaims
          .Where(ldapRole => !loginUser.Roles.Where(role => CanAddRole(site, role) && role.Name.Equals(ldapRole.Value, StringComparison.OrdinalIgnoreCase)).Any()))
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

  public static LdapConnection BuildLdapConnection(AuthenticationProtocol protocolOptions, ILogger logger)
  {
    LdapConnection connection;

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
    // connection.Timeout = TimeSpan.FromSeconds(10);  

    logger?.LogTrace("Binding LDAP connection.");
    connection.Bind();
    logger?.LogTrace("Bind complete.");


    return connection;
  }

  private static List<Claim> GetLdapUserProperties(LdapConnection connection, string userSid)
  {
    List<Claim> results = new();
    string ldapFilter = $"(objectSid={userSid})";

    string dn = String.Join(',',
      ((LdapDirectoryIdentifier)connection.Directory).Servers
        .FirstOrDefault()
        ?.Split('.', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => $"DC={part}"));


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
                // "memberof" has entries in the form:
                // CN=role name,<various other stuff like OU, DC, etc>;CN=role name2,<more stuff>
                IEnumerable<string> roleLdapInfo = memberOf.Split(',', StringSplitOptions.RemoveEmptyEntries);

                string roleCommonName = roleLdapInfo.Where(value => value.StartsWith("CN=")).FirstOrDefault();
                if (roleCommonName != null)
                {
                  string roleName = roleCommonName.Substring("CN=".Length);
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

    return results;
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
