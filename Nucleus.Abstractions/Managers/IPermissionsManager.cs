using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers;

/// <summary>
/// Defines the interface for the permissions manager.
/// </summary>
/// <remarks>
/// Get an instance of this class from dependency injection by including a parameter in your class constructor.  A general design principle 
/// for Manager classes in Nucleus is that they should not include another Manager class as a dependency to avoid circular references, but the 
/// PermissionsManager (this class) is one of two exceptions to that rule, the other one being <see cref="ICacheManager"/>.
/// </remarks>
public interface IPermissionsManager
{
  /// <summary>
  /// Create a new permission type.
  /// </summary>
  /// <param name="permissionType"></param>
  /// <returns></returns>
  /// <remarks>
  /// If the permission type already exists, this function does nothing, and does not throw an exception.
  /// </remarks>
  abstract Task AddPermissionType(PermissionType permissionType);

  /// <summary>
  /// List all permission types.
  /// </summary>
  /// <param name="scopeNamespace"></param>
  /// <returns></returns>
  abstract Task<List<PermissionType>> ListPermissionTypes(string scopeNamespace);

  /// <summary>
  /// List all permissions for the entity specified by Id.
  /// </summary>
  /// <param name="relatedId"></param>
  /// <param name="permissionNameSpace"></param>
  /// <returns></returns>
  abstract Task<List<Permission>> ListPermissions(Guid relatedId, string permissionNameSpace);

  /// <summary>
  /// Delete all permissions for the entity specified by Id.
  /// </summary>
  /// <param name="permissions"></param>
  /// <returns></returns>
  abstract Task DeletePermissions(IEnumerable<Permission> permissions);

  /// <summary>
  /// Save permissions for the entity specified by <paramref name="relatedId"/>, and delete any existing permissions which exist in <paramref name="originalPermissions"/> but no not exist in
  /// <paramref name="permissions"/>.
  /// </summary>
  /// <param name="relatedId"></param>
  /// <param name="permissions"></param>
  /// <param name="originalPermissions"></param>
  /// <returns></returns>
  abstract Task SavePermissions(Guid relatedId, IEnumerable<Permission> permissions, IList<Permission> originalPermissions);
}
