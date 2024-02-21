using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="Permission"/> and <see cref="PermissionType"/> classes.
	public interface IPermissionsDataProvider : IDisposable//, IDataProvider<IPermissionsDataProvider>
	{
		abstract Task<List<PermissionType>> ListPermissionTypes(string scopeNamespace);
		abstract Task<Guid> AddPermissionType(PermissionType permissionType);

		abstract Task<List<Permission>> ListPermissions(Guid Id, string permissionNameSpace);
		abstract Task DeletePermission(Permission permission);

		abstract Task SavePermission(Permission permission);

	}
}
