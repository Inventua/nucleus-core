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
using Nucleus.Extensions.Authorization;

namespace Nucleus.Modules.Forums;

/// <summary>
/// Provides functions to manage database data for <see cref="Group"/>s.
/// </summary>
public class GroupsManager
{
	private IDataProviderFactory DataProviderFactory { get; }
	private ICacheManager CacheManager { get; }
	private IPermissionsManager PermissionsManager { get; }
	private ForumsManager ForumsManager { get; }

	public GroupsManager(IDataProviderFactory dataProviderFactory, ForumsManager forumsManager, ICacheManager cacheManager, IPermissionsManager permissionsManager)
	{
		this.ForumsManager = forumsManager;
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

					result.Forums = new List<Forum>();
					foreach (Guid forumId in await provider.ListForums(result))
					{
						result.Forums.Add(await this.ForumsManager.Get(forumId));
					}
					await CheckPermissionsExist(result);
				}

				return result;
			}
		});
	}

	public async Task CheckPermissionsExist(Group group)
	{
		// Forums and groups have the same permissions namespace, so we use Forum.URN instead of Group.URN
		List<PermissionType> permissionTypes = await this.PermissionsManager.ListPermissionTypes(Forum.URN);
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

	public Boolean CheckPermission(Site site, ClaimsPrincipal user, Group group, string permissionScope)
	{
		if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
		{
			return true;
		}
		else
		{
			if (!user.IsApproved() || !user.IsVerified())
			{
				// if the user is not approved/verified, they don't have permission
				return false;
			}
			else
			{
				foreach (Permission permission in group.Permissions)
				{
					if (permission.PermissionType.Scope == permissionScope)
					{
						if (permission.IsValid(site, user))
						{
							return true;
						}
					}
				}
			}
		}

		return false;
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
	public async Task<IEnumerable<Group>> List(PageModule module)
	{
		using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
		{
			return await provider.ListGroups(module);
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
			// save a copy of the group with forums set to null so that EF doesn't try to save forums
			Group saveGroup = group.Copy<Group>();
			saveGroup.Forums = null;
			await provider.SaveGroup(module, saveGroup);
			group.Id = saveGroup.Id;

			if (group.Permissions != null)
			{
				// save permissions
				List<Permission> originalPermissions = await this.PermissionsManager.ListPermissions(group.Id, ForumsManager.PermissionScopeNamespaces.Forum);
				await this.PermissionsManager.SavePermissions(group.Id, group.Permissions, originalPermissions);
			}

			this.CacheManager.GroupsCache().Remove(group.Id);

			// forums can inherit group settings, so we need to expire them too
			if (group.Forums?.Any() == true)
			{
				foreach (Forum forum in group.Forums)
				{
					this.CacheManager.ForumsCache().Remove(forum.Id);
				}
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

	public async Task<Guid?> FindForum(PageModule module, string encodedName)
	{
		using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
		{
			foreach (Models.Group group in await this.List(module))
			{
				foreach (Guid forumId in await provider.ListForums(group))
				{
					Forum forum = await this.ForumsManager.Get(forumId);
					if (forum.Name.FriendlyEncode().Equals(encodedName, StringComparison.OrdinalIgnoreCase))
					{
						return forum.Id;
					}
				}
			}
		}

		return default;
	}

	/// <summary>
	/// Update the <see cref="Page.SortOrder"/> of the page module specifed by id by swapping it with the next-highest <see cref="Page.SortOrder"/>.
	/// </summary>
	/// <param name="pageId"></param>
	public async Task MoveDown(PageModule pageModule, Guid groupId)
	{
		Group previousGroup = null;
		//Group thisGroup;
		IEnumerable<Group> siblingGroups;

		using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
		{
			//thisGroup = await this.Get(groupId);
			siblingGroups = await provider.ListGroups(pageModule);
		}

		await CheckNumbering(pageModule, siblingGroups);

		siblingGroups = siblingGroups.Reverse();
		foreach (Group group in siblingGroups)
		{
			if (group.Id == groupId)
			{
				if (previousGroup != null)
				{
					int temp = group.SortOrder;
					group.SortOrder = previousGroup.SortOrder;
					previousGroup.SortOrder = temp;

					if (previousGroup.SortOrder == group.SortOrder)
					{
						group.SortOrder += 10;
					}

					using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
					{
						await provider.SaveGroup(pageModule, previousGroup);
					}

					using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
					{
						await provider.SaveGroup(pageModule, group);
					}

					this.CacheManager.GroupsCache().Remove(group.Id);
					this.CacheManager.GroupsCache().Remove(previousGroup.Id);

					break;
				}
			}
			else
			{
				previousGroup = group;
			}
		}
	}


	/// <summary>
	/// Update the <see cref="Group.SortOrder"/> of the group module specifed by id by swapping it with the previous <see cref="Group.SortOrder"/>.
	/// </summary>
	/// <param name="groupId"></param>
	public async Task MoveUp(PageModule pageModule, Guid groupId)
	{
		Group previousGroup = null;
		//Group thisGroup;
		IEnumerable<Group> siblingGroups;

		using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
		{
			//thisGroup = await provider.GetGroup(groupId);
			siblingGroups = await provider.ListGroups(pageModule);
		}

		await CheckNumbering(pageModule, siblingGroups);

		foreach (Group group in siblingGroups)
		{
			if (group.Id == groupId)
			{
				if (previousGroup != null)
				{
					int temp = group.SortOrder;
					group.SortOrder = previousGroup.SortOrder;
					previousGroup.SortOrder = temp;

					if (previousGroup.SortOrder == group.SortOrder)
					{
						previousGroup.SortOrder += 10;
					}

					using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
					{
						await provider.SaveGroup(pageModule, previousGroup);
					}

					using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
					{
						await provider.SaveGroup(pageModule, group);
					}

					this.CacheManager.GroupsCache().Remove(group.Id);
					this.CacheManager.GroupsCache().Remove(previousGroup.Id);
					break;
				}
			}
			else
			{
				previousGroup = group;
			}
		}
	}


	/// <summary>
	/// Ensure that pages have unique sort order.
	/// </summary>
	/// <param name="pages"></param>
	/// <remarks>
	/// Page sort orders can produce duplicates and gaps when pages parents are changed, or pages are deleted.
	/// </remarks>
	private async Task CheckNumbering(PageModule pageModule, IEnumerable<Group> groups)
	{
		int sortOrder = 10;

		using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
		{
			foreach (Group group in groups)
			{
				if (group.SortOrder != sortOrder)
				{
					group.SortOrder = sortOrder;
					await provider.SaveGroup(pageModule, group);

					this.CacheManager.GroupsCache().Remove(group.Id);
				}

				sortOrder += 10;
			}
		}
	}
}
