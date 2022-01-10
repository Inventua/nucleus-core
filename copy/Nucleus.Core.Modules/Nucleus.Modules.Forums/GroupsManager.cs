using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Modules.Forums.DataProviders;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Group"/>s.
	/// </summary>
	public class GroupsManager
	{
		

		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }

		public GroupsManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;

			// todo: get cacheoption settings from config
			this.CacheManager.Add<Guid, Group>(new Nucleus.Abstractions.Models.Configuration.CacheOption());
		}

		/// <summary>
		/// Create a new <see cref="Group"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Group"/> is not saved to the database until you call <see cref="Save(PageModule, Group)"/>.
		/// </remarks>
		public Group Create()
		{
			Group result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Group"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Group Get(Guid id)
		{
			return this.CacheManager.GroupsCache().Get(id, id =>
			{
				using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
				{
					Group result = provider.GetGroup(id);
					if (result != null)
					{
						result.Forums = provider.ListForums(result);
						using (IPermissionsDataProvider coreProvider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
						{
							result.Permissions = coreProvider.ListPermissions(id, ForumsManager.PermissionScopeNamespaces.Forum);
						}
					}
					return result;
				}
			});
		}

		/// <summary>
		/// Delete the specifed <see cref="Group"/> from the database.
		/// </summary>
		/// <param name="Groups"></param>
		public void Delete(Group Groups)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.DeleteGroup(Groups);
				this.CacheManager.GroupsCache().Remove(Groups.Id);
			}
		}

		/// <summary>
		/// List all <see cref="Group"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IList<Group> List(PageModule module)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return provider.ListGroups(module);
			}
		}

		/// <summary>
		/// Create or update a <see cref="Group"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="Groups"></param>
		public void Save(PageModule module, Group group)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.SaveGroup(module, group);

				if (group.Permissions != null)
				{
					// save permissions
					using (IPermissionsDataProvider permissionsProvider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
					{
						List<Permission> originalPermissions = permissionsProvider.ListPermissions(group.Id, ForumsManager.PermissionScopeNamespaces.Forum);
						permissionsProvider.SavePermissions(group.Id, group.Permissions, originalPermissions);
					}
				}
				
				this.CacheManager.GroupsCache().Remove(group.Id);
			}
		}

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public List<PermissionType> ListForumPermissionTypes()
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return provider.ListPermissionTypes(ForumsManager.PermissionScopeNamespaces.Forum).OrderBy(permissionType => permissionType.SortOrder).ToList();
			}
		}

		/// <summary>
		/// Add default permissions to the specifed <see cref="Group"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="group"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new permissions are not saved unless you call <see cref="Save(Site, Page)"/>.
		/// </remarks>
		public void CreatePermissions(Group group, Role role)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				{
					List<PermissionType> permissionTypes = provider.ListPermissionTypes(ForumsManager.PermissionScopeNamespaces.Forum);
					List<Permission> permissions = new();

					foreach (PermissionType permissionType in permissionTypes)
					{
						Permission permission = new();
						permission.AllowAccess = false;
						permission.PermissionType = permissionType;
						permission.Role = role;

						permissions.Add(permission);
					}

					group.Permissions.AddRange(permissions);
				}
			}
		}

		public IList<Permission> ListPermissions(Group group)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return provider.ListPermissions(group.Id, ForumsManager.PermissionScopeNamespaces.Forum);
			}
		}

		public Forum FindForum(PageModule module, string encodedName)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{

				foreach (Models.Group group in this.List(module))
				{
					foreach (Models.Forum forum in provider.ListForums(group))
					{
						if (forum.EncodedName().Equals(encodedName, StringComparison.OrdinalIgnoreCase))
						{
							return forum;
						}
					}
				}
			}

			return null;
		}
	}
}
