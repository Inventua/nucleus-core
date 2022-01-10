using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core.Managers
{
	public class PermissionsManager : IPermissionsManager
	{
		private IDataProviderFactory DataProviderFactory { get; }

		public PermissionsManager(IDataProviderFactory dataProviderFactory)
		{
			this.DataProviderFactory = dataProviderFactory;
		}

		public async Task<List<Permission>> ListPermissions(Guid Id, string permissionNameSpace)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return await provider.ListPermissions(Id, permissionNameSpace);
			}
		}

		public async Task<List<PermissionType>> ListPermissionTypes(string scopeNamespace)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return await provider.ListPermissionTypes(scopeNamespace);				
			}
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
		}
	}
}
