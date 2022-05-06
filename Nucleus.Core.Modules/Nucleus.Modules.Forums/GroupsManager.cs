using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Nucleus.Data.Common;
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

		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }
		private IPermissionsManager PermissionsManager { get; }

		public GroupsManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, IPermissionsManager permissionsManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.PermissionsManager = permissionsManager;
		}

		/// <summary>
		/// Create a new <see cref="Group"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Group"/> is not saved to the database until you call <see cref="Save(PageModule, Group)"/>.
		/// </remarks>
		public Task<Group> Create()
		{
			Group result = new();
			result.Settings = new();
			return Task.FromResult(result);
		}

		/// <summary>
		/// Retrieve an existing <see cref="Group"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Group> Get(Guid id)
		{
			return await this.CacheManager.GroupsCache().GetAsync(id, async id =>
			{
				using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
				{
					Group result = await provider.GetGroup(id);
					if (result != null)
					{
						if (result.Settings == null)
						{
							result.Settings = new();
						}
						result.Forums = await provider.ListForums(result);						
						await CheckPermissions(result);
					}
					return result;
				}
			});
		}

		private async Task CheckPermissions(Group group)
		{
			List<PermissionType> permissionTypes = await this.PermissionsManager.ListPermissionTypes(Group.URN);
			Dictionary<Role, IList<Permission>> results = new();

			// ensure that for each role with any permissions defined, there is a full set of permission types for the role
			foreach (Role role in group.Permissions.Select((permission) => permission.Role).ToList())
			{
				foreach (PermissionType permissionType in permissionTypes)
				{
					if (group.Permissions.Where((permission) => permission?.Role.Id == role.Id && permission?.PermissionType.Id == permissionType.Id).ToList().Count == 0)
					{
						Permission permission = new();
						permission.AllowAccess = false;
						permission.PermissionType = permissionType;
						permission.Role = role;
						group.Permissions.Add(permission);
					}
				}
			}
		}

		/// <summary>
		/// Delete the specifed <see cref="Group"/> from the database.
		/// </summary>
		/// <param name="Groups"></param>
		public async Task Delete(Group group)
		{
			await this.PermissionsManager.DeletePermissions(group.Permissions);
			
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.DeleteGroup(group);
				this.CacheManager.GroupsCache().Remove(group.Id);
			}
		}

		/// <summary>
		/// List all <see cref="Group"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IList<Group>> List(PageModule module)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return await provider .ListGroups(module);
			}
		}

		/// <summary>
		/// Create or update a <see cref="Group"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="Groups"></param>
		public async Task Save(PageModule module, Group group)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SaveGroup(module, group);

				if (group.Permissions != null)
				{
					// save permissions
					List<Permission> originalPermissions = await this.PermissionsManager.ListPermissions(group.Id, ForumsManager.PermissionScopeNamespaces.Forum);
					await this.PermissionsManager.SavePermissions(group.Id, group.Permissions, originalPermissions);
				}

				this.CacheManager.GroupsCache().Remove(group.Id);
				// forums can inherit group settings, so we need to expire them too
				foreach (Forum forum in group.Forums)
				{
					this.CacheManager.ForumsCache().Remove(forum.Id);
				}
			}
		}

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public async Task<List<PermissionType>> ListForumPermissionTypes()
		{
			return (await this.PermissionsManager.ListPermissionTypes(ForumsManager.PermissionScopeNamespaces.Forum))
				.OrderBy(permissionType => permissionType.SortOrder)
				.ToList();
		}

		/// <summary>
		/// Add default permissions to the specifed <see cref="Group"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="group"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new permissions are not saved unless you call <see cref="Save(Site, Page)"/>.
		/// </remarks>
		public async Task CreatePermissions(Group group, Role role)
		{
			List<PermissionType> permissionTypes = await this.PermissionsManager.ListPermissionTypes(ForumsManager.PermissionScopeNamespaces.Forum);
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

		//public async Task<IList<Permission>> ListPermissions(Group group)
		//{
		//	return await this.PermissionsManager.ListPermissions(group.Id, ForumsManager.PermissionScopeNamespaces.Forum);
		//}

		public async Task<Forum> FindForum(PageModule module, string encodedName)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{

				foreach (Models.Group group in await this.List(module))
				{
					foreach (Models.Forum forum in await provider.ListForums(group))
					{
						if (forum.Name.FriendlyEncode().Equals(encodedName, StringComparison.OrdinalIgnoreCase))
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
