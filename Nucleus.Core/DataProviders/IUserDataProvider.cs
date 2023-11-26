using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="User"/>, <see cref="Role"/> and <see cref="RoleGroup"/> classes.
	public interface IUserDataProvider : IDisposable//, IDataProvider<IUserDataProvider>
	{
		abstract Task<IList<User>> ListUsers(Site site);
		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListUsers(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
    abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListUsers(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings, Expression<Func<User, bool>> filterExpression);
    

    abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> SearchUsers(Site site, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
    abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> SearchUsers(Site site, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings, Expression<Func<User, bool>> filterExpression);
    

    abstract Task<User> GetUser(Guid userId);
		abstract Task<User> GetUserByName(Site site, string userName);
		abstract Task<User> GetUserByEmail(Site site, string email);

		abstract Task SaveUser(Site site, User user);
		abstract Task DeleteUser(User user);
		abstract Task SaveUserSecrets(User user);


		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListSystemAdministrators(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);		
		abstract Task<long> CountSystemAdministrators();

		abstract Task SaveSystemAdministrator(User user);
		abstract Task<User> GetSystemAdministrator(string userName);

		abstract Task<IEnumerable<RoleGroup>> ListRoleGroups(Site site);

		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<RoleGroup>> ListRoleGroups(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		abstract Task<RoleGroup> GetRoleGroup(Guid roleGroupId);
    abstract Task<RoleGroup> GetRoleGroupByName(Site site, string name);
    abstract Task SaveRoleGroup(Site site, RoleGroup roleGroup);
		abstract Task DeleteRoleGroup(RoleGroup roleGroup);

		abstract Task<List<Role>> ListRoleGroupRoles(Guid RoleGroupId);
		abstract Task<IEnumerable<Role>> ListRoles(Site site);
		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<Role>> ListRoles(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		abstract Task<Role> GetRole(Guid roleId);
		abstract Task<Role> GetRoleByName(string name);
		abstract Task SaveRole(Site site, Role role);
		abstract Task DeleteRole(Role role);

		abstract Task<IList<User>> ListUsersInRole(Guid roleId);

		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListUsersInRole(Guid roleId, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
	}
}
