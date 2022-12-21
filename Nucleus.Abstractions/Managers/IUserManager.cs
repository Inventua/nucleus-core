using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="User"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IUserManager
	{
		/// <summary>
		/// Check that the specified password is valid.
		/// </summary>
		/// <param name="key">The key for model state errors.  This is generally the name of the model property being validated.</param>
		/// <param name="password"></param>
		/// <returns></returns>
		/// <remarks>
		/// Each <see cref="PasswordComplexityRule"/> is executed, and the results are returned in a ModelStateDictionary.
		/// </remarks>
		public Task<Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary> ValidatePasswordComplexity(string key, string password);

		/// <summary>
		/// Verify that the user's password is correct
		/// </summary>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function also manages failed login tracking, account suspension
		/// </remarks>
		public Task<Boolean> VerifyPassword(User user, string password);

		/// <summary>
		/// Generate and set a random password reset token.
		/// </summary>
		/// <param name="user"></param>
		public Task SetPasswordResetToken(User user);

		/// <summary>
		/// Generate and set a random verification token.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public Task SetVerificationToken(User user);

		/// <summary>
		/// Create a new <see cref="User"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="User"/> to the database.  Call <see cref="Save(Site, User)"/> to save the role group.
		/// </remarks>
		public Task<User> CreateNew(Site site);

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<User> Get(Site site, Guid id);

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<User> Get(Guid id);

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database, matching by the specified userName.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="userName"></param>
		/// <returns></returns>
		public Task<User> Get(Site site, string userName);

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database, matching by the specified email address.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="email"></param>
		/// <returns></returns>
		public Task<User> GetByEmail(Site site, string email);


		/// <summary>
		/// Retrieve an existing System Administrator <see cref="User"/> from the database, matching by the specified userName.
		/// </summary>
		/// <param name="userName"></param>
		/// <returns></returns>
		public Task<User> GetSystemAdministrator(string userName);

		/// <summary>
		/// Add the specified <see cref="Role"/> to the specified <see cref="User"/>.  
		/// </summary>
		/// <param name="user"></param>
		/// <param name="roleId"></param>
		/// <remarks>
		/// Changes are not saved to the database.  Call the <see cref="Save(Site, User)"/> method to save changes.
		/// </remarks>
		public Task AddRole(User user, Guid roleId);

		/// <summary>
		/// Removes the specified <see cref="Role"/> to the specified <see cref="User"/>.  
		/// </summary>
		/// <param name="user"></param>
		/// <param name="roleId"></param>
		/// <remarks>
		/// Changes are not saved to the database.  Call the <see cref="Save(Site, User)"/> method to save changes.
		/// </remarks>
		public Task RemoveRole(User user, Guid roleId);

		/// <summary>
		/// List all <see cref="User"/>s who belong to the specified <see cref="Site"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public Task<IList<User>> List(Site site);

		/// <summary>
		/// List a page of <see cref="User"/>s who belong to the specified <see cref="Site"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// List all System Administrator <see cref="User"/>s.
		/// </summary>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListSystemAdministrators(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Count the number of System Administrator <see cref="User"/>s.
		/// </summary>
		/// <returns></returns>
		public Task<long> CountSystemAdministrators();

		/// <summary>
		/// Returns a list of <see cref="User"/>s whi match the specified searchTerm.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="searchTerm"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> Search(Site site, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Create or update the specified <see cref="User"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="user"></param>
		public Task Save(Site site, User user);

		/// <summary>
		/// Set <see cref="User"/> approved and verified flags based on site settings.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="user"></param>
		/// <remarks>
		/// This function should be called before saving a new user, if the user is self-registering.
		/// </remarks>
		public void SetNewUserFlags(Site site, User user);

		/// <summary>
		/// Save user secrets.
		/// </summary>
		/// <param name="user"></param>
		public Task SaveSecrets(User user);


		/// <summary>
		/// Create or update the specified System Administrator <see cref="User"/>.
		/// </summary>
		/// <param name="user"></param>
		public Task SaveSystemAdministrator(User user);

		/// <summary>
		/// Delete the specified <see cref="User"/> from the database.
		/// </summary>
		/// <param name="user"></param>
		public Task Delete(User user);

		/// <summary>
		/// List the <see cref="User"/>s who are members of the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		public Task<IList<User>> ListUsersInRole(Role role);

		/// <summary>
		/// List the <see cref="User"/>s who are members of the specified <see cref="Role"/> with paging.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListUsersInRole(Role role, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
	}
}
