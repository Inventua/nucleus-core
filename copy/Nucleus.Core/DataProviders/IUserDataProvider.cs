using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="User"/>, <see cref="Role"/> and <see cref="RoleGroup"/> classes.
	public interface IUserDataProvider : IDisposable, Abstractions.IDataProvider
	{
		abstract List<User> ListUsers(Site site);
		abstract List<User> SearchUsers(Site site, string searchTerm);
		abstract User GetUser(Guid userId);
		abstract User GetUserByName(Site site, string userName);
		abstract User GetUserByEmail(Site site, string email);

		abstract void SaveUser(Site site, User user);
		abstract void DeleteUser(User user);
		abstract void SaveUserSecrets(User user);


		abstract List<User> ListSystemAdministrators();
		abstract void SaveSystemAdministrator(User user);
		abstract User GetSystemAdministrator(string userName);

		abstract List<RoleGroup> ListRoleGroups(Site site);
		abstract RoleGroup GetRoleGroup(Guid roleGroupId);
		abstract void SaveRoleGroup(Site site, RoleGroup roleGroup);
		abstract void DeleteRoleGroup(RoleGroup roleGroup);

		abstract List<Role> ListUserRoles(Guid userId);
		abstract List<Role> ListRoleGroupRoles(Guid RoleGroupId);
		abstract List<Role> ListRoles(Site site);
		abstract Role GetRole(Guid roleId);
		abstract void SaveRole(Site site, Role role);
		abstract void DeleteRole(Role role);
	}
}
