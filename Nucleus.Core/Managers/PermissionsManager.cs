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

namespace Nucleus.Core.Managers
{
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
        List<Permission> results =  await provider.ListPermissions(Id, permissionNameSpace);
        
        // make sure the permissions list is fully populated (in case more permission types for the entity have been added since
        // it was created)
        foreach (PermissionType permissionType in permissionTypes)
        {
          foreach (Role role in results.Select(permissions => permissions.Role).ToList())
          {
            if (!results.Where(permission => permission.PermissionType.Scope == permissionType.Scope && permission.Role.Id == role.Id).Any())
            {
              // a role does not have an available permission, add 
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
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				await provider.SavePermissions(relatedId, permissions, originalPermissions);
			}
		}

		public async Task DeletePermissions(IEnumerable<Permission> permissions)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				await provider.DeletePermissions(permissions);
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
}
