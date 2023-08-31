using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Represents a configured authentication protocol.
/// </summary>
public class AuthenticationProtocol
{
  /// <summary>
  /// Values used to specify whether system admin, site admin or regular users can log in
  /// </summary>
  [Flags]
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum UserTypes
  {
    /// <summary>
    /// Specifies that users can log in
    /// </summary>
    Users = 1,
    /// <summary>
    /// Specifies that site admins can log in
    /// </summary>
    SiteAdmins = 2,
    /// <summary>
    /// Specifies that system admins can log in
    /// </summary>
    SystemAdmins = 4
  }

  /// <summary>
  /// Enumeration used to specify what data is copied to the Nucleus user from the authentication source when logging in.
  /// </summary>
  [Flags]
  public enum SyncOptions
  {
    /// <summary>
    /// Specifies that data is not updated during login.
    /// </summary>
    None = 0,
    /// <summary>
    /// Specifies that user profile properties are copied from the authentication source.
    /// </summary>
    Profile = 1,
    /// <summary>
    /// Specifies that roles are copied from the authentication source.
    /// </summary>
    Roles = 2
  }

  /// <summary>
  /// Specifies the Authentication Protocol.  
  /// </summary>
  /// <remarks>
  /// Valida values are: Negotiate (for Kerberos/Windows authentication)
  /// </remarks>
  public string Scheme{ get; private set; }

  /// <summary>
  /// Display name for the authentication protocol.
  /// </summary>
  public string FriendlyName { get; private set; }

  /// <summary>
  /// Specifies whether the protocol is enabled.
  /// </summary>
  public Boolean Enabled { get; private set; }

  /// <summary>
  /// Specifies whether the protocol is automatically attempted (by the login module) when a user navigates to a page containing the login module.
  /// </summary>
  public Boolean AutomaticLogon { get; private set; } = true;

  /// <summary>
  /// Specifies LDAP domain name.  Optional.
  /// </summary>
  public string Domain { get; private set; }

  /// <summary>
  /// Specifies the account name to use for LDAP queries.  Optional.
  /// </summary>
  public string MachineAccountName { get; private set; }

  /// <summary>
  /// Specifies the password to use for LDAP queries.  Optional.
  /// </summary>
  public string MachineAccountPassword { get; private set; }

  /// <summary>
  /// Specifies whether to create a Nucleus user for authenticated users, if there is no existing account for the user.
  /// </summary>
  public Boolean CreateUsers { get; private set; } = false;

  /// <summary>
  /// Specifies whether to update the Nucleus user with data from the authentication provider during each login.
  /// </summary>
  public SyncOptions UserSyncOptions { get; private set; } = AuthenticationProtocol.SyncOptions.None;

  /// <summary>
  /// Specifies whether to remove the domain name from the authenticated user name when checking for and creating a Nucleus user account.
  /// </summary>
  public Boolean UserRemoveDomainName { get; private set; } = false;

  /// <summary>
  /// Specifies which classes of users can log in with the authentication protocol.
  /// </summary>
  public UserTypes AllowedUsers { get; private set; }
}
