using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="Permission"/> and <see cref="PermissionType"/> classes.
	public interface IPermissionsDataProvider : IDisposable, Abstractions.IDataProvider
	{
		abstract List<PermissionType> ListPagePermissionTypes();
		abstract List<Permission> ListPagePermissions(Guid pageId);
		abstract List<PermissionType> ListModulePermissionTypes();
		abstract List<Permission> ListPageModulePermissions(Guid moduleId);
		public List<PermissionType> ListFolderPermissionTypes();
		abstract List<Permission> ListFolderPermissions(Guid folderId);
		abstract List<PermissionType> ListPermissionTypes(string scopeNamespace);
		abstract List<Permission> ListPermissions(Guid Id, string permissionNameSpace);

		abstract void SavePagePermissions(Guid pageId, IEnumerable<Permission> permissions, IEnumerable<Permission> originalPermissions);
		abstract void SavePageModulePermissions(Guid moduleId, IEnumerable<Permission> permissions, IEnumerable<Permission> originalPermissions);
		abstract void SaveFolderPermissions(Guid folderId, IEnumerable<Permission> permissions, IEnumerable<Permission> originalPermissions);
		abstract void SavePermissions(Guid relatedId, IEnumerable<Permission> permissions, IEnumerable<Permission> originalPermissions);

	}
}
