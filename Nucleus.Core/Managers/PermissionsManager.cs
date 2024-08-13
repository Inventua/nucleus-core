using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Nucleus.Core.Managers;

public class PermissionsManager : IPermissionsManager
{
  private IDataProviderFactory DataProviderFactory { get; }
  private ICacheManager CacheManager { get; }

  public PermissionsManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
  {
    this.DataProviderFactory = dataProviderFactory;
    this.CacheManager = cacheManager;
  }

  public async Task<List<Permission>> ListPermissions(Guid Id, string permissionNameSpace)
  {
    using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
    {
      List<PermissionType> permissionTypes = await ListPermissionTypes(permissionNameSpace);
      List<Permission> results = await provider.ListPermissions(Id, permissionNameSpace);

      // make sure the permissions list is fully populated (in case more permission types for the entity have been added since
      // it was created)
      foreach (PermissionType permissionType in permissionTypes)
      {
        foreach (Role role in results.Select(permissions => permissions.Role).ToList())
        {
          if (!results.Where(permission => permission.PermissionType.Scope == permissionType.Scope && permission.Role.Id == role.Id).Any())
          {
            // a role does not have an available permission, add a placeholder
            results.Add(new Permission()
            {              
              AllowAccess = false,
              Role = role,
              PermissionType = permissionType
            });
          }
        }
      }

      return results;
    }
  }

  public async Task<List<PermissionType>> ListPermissionTypes(string scopeNamespace)
  {
    return await this.CacheManager.PermissionTypesCache().GetAsync(scopeNamespace, async scopeNamespace =>
    {
      using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
      {
        return await provider.ListPermissionTypes(scopeNamespace);
      }
    });
  }

  public async Task SavePermissions(Guid relatedId, IEnumerable<Permission> permissions, IList<Permission> originalPermissions)
  {
    List<Permission> deletedPermissions = [];

    // delete removed permissions
    foreach (Permission oldPermission in originalPermissions)
    {
      if (!permissions.Where(existing => existing.Id == oldPermission.Id || (existing.Role.Id == oldPermission.Role.Id && existing.PermissionType.Id == oldPermission.PermissionType.Id)).Any())
      {
        deletedPermissions.Add(oldPermission);
      }
    }

    if (deletedPermissions.Any())
    {
      await DeletePermissions(deletedPermissions);
    }

    // add/update permissions
    foreach (Permission newPermission in permissions.Where(permission => !permission.IsDisabled))
    {
      using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
      {
        newPermission.RelatedId = relatedId;
        await provider.SavePermission(newPermission);
      }
    }
  }

  public async Task DeletePermissions(IEnumerable<Permission> permissions)
  {
    foreach (Permission permission in permissions)
    {
      using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
      {
        await provider.DeletePermission(permission);
      }
    }
  }

  public async Task AddPermissionType(PermissionType permissionType)
  {
    using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
    {
      await provider.AddPermissionType(permissionType);
    }
    this.CacheManager.PermissionTypesCache().Remove(permissionType.Scope);
  }
}
