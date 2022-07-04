using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="User"/>s.
	/// </summary>
	public class UserManager : IUserManager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private PasswordOptions PasswordOptions { get; }
		private ClaimTypeOptions ClaimTypeOptions { get; }
		private ICacheManager CacheManager { get; }
		private ILogger<UserManager> Logger { get; }

		public UserManager(ILogger<UserManager> logger, IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, IOptions<PasswordOptions> passwordOptions, IOptions<ClaimTypeOptions> claimTypeOptions)
		{
			this.Logger = logger;
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.PasswordOptions = passwordOptions.Value;
			this.ClaimTypeOptions = claimTypeOptions.Value;
		}

		/// <summary>
		/// Check that the specified password is valid.
		/// </summary>
		/// <param name="key">The key for model state errors.  This is generally the name of the model property being validated.</param>
		/// <param name="password"></param>
		/// <returns></returns>
		/// <remarks>
		/// Each <see cref="PasswordComplexityRule"/> is executed, and the results are returned in a ModelStateDictionary.
		/// </remarks>
		public Task<Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary> ValidatePasswordComplexity(string key, string password)
		{
			Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = new();

			if (this.PasswordOptions.PasswordComplexityRules != null)
			{
				foreach (PasswordComplexityRule rule in this.PasswordOptions.PasswordComplexityRules)
				{
					try
					{
						if (!System.Text.RegularExpressions.Regex.IsMatch(password, rule.Pattern))
						{
							modelState.AddModelError(key, rule.Message);
						}
					}
					catch (Exception e)
					{
						modelState.AddModelError(key, $"Unable to process password complexity rule '{rule?.Message}': {e.Message}");
					}
				}
			}

			return Task.FromResult(modelState);
		}

		/// <summary>
		/// Verify that the user's password is correct
		/// </summary>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function also manages failed login tracking, account suspension
		/// </remarks>
		public async Task<Boolean> VerifyPassword(User user, string password)
		{
			if (user.Secrets == null)
      {
				this.Logger.LogInformation("Login denied for user {0} - user secrets not set.", user.UserName);
				return false;
      }

			if (user.Secrets.IsLockedOut)
			{
				if (user.Secrets.LastLockoutDate < DateTime.UtcNow.Subtract(this.PasswordOptions.FailedPasswordLockoutReset))					
				{
					// lockout time has passed, allow login attempt.  The account will be unlocked only if the login attempt is successful
				}
				else
				{
					this.Logger.LogInformation("Login denied for user {0} - user is locked out.", user.UserName);
					// user is locked out.  We don't inform them that they are locked out, the response is the same as a failed password
					return false;
				}
			}

			if (String.IsNullOrEmpty(password) || !user.Secrets.VerifyPassword(password))
			{
				if (user.Secrets.FailedPasswordWindowStart < DateTime.UtcNow.Subtract(this.PasswordOptions.FailedPasswordWindowTimeout))
				{
					user.Secrets.FailedPasswordWindowStart = DateTime.UtcNow;
					user.Secrets.FailedPasswordAttemptCount = 1;

					this.Logger.LogInformation("Login failed for user {0} - attempt {1}.", user.UserName, user.Secrets.FailedPasswordAttemptCount);
				}
				else
				{
					user.Secrets.FailedPasswordAttemptCount += 1;
					this.Logger.LogInformation("Login failed for user {0} - attempt {1}.", user.UserName, user.Secrets.FailedPasswordAttemptCount);

					if (user.Secrets.FailedPasswordAttemptCount >= this.PasswordOptions.FailedPasswordMaxAttempts)
					{
						user.Secrets.LastLockoutDate = DateTime.UtcNow;
						user.Secrets.IsLockedOut = true;
						this.Logger.LogInformation("User {0} - locked out.", user.UserName);
					}
				}

				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					await provider.SaveUserSecrets(user);
				}

				return false;
			}
			else
			{
				if (user.Secrets.IsLockedOut)
				{
					this.Logger.LogInformation("User {0} account unlocked.", user.UserName);
					user.Secrets.IsLockedOut = false;
				}

				user.Secrets.FailedPasswordWindowStart = null;
				user.Secrets.FailedPasswordAttemptCount = 0;
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					await provider.SaveUserSecrets(user);
				}

				return true;
			}
		}

		public async Task SetVerificationToken(User user)
		{
			user.Secrets.VerificationToken = new Random().Next(100000, 999999).ToString();
			user.Secrets.VerificationTokenExpiryDate = DateTime.UtcNow.Add(this.PasswordOptions.VerificationTokenExpiry);

			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				await provider.SaveUserSecrets(user);
			}
		}

		public async Task SetPasswordResetToken(User user)
		{
			if (user.Secrets == null)
			{
				user.Secrets = new();
				user.Secrets.SetPassword(Guid.NewGuid().ToString());
			}	

			user.Secrets.PasswordResetToken = new Random().Next(100000, 999999).ToString();
			user.Secrets.PasswordResetTokenExpiryDate = DateTime.UtcNow.Add(this.PasswordOptions.PasswordResetTokenExpiry);
			
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				await provider.SaveUserSecrets(user);
			}
		}

		/// <summary>
		/// Create a new <see cref="User"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="User"/> to the database.  Call <see cref="Save(Site, User)"/> to save the role group.
		/// </remarks>
		public async Task<User> CreateNew(Site site)
		{
			User result = new();
			result.Roles = new();

			// add new user to registered users role
			if (site.RegisteredUsersRole != null)
			{
				result.Roles.Add(site.RegisteredUsersRole);
			}

			// add auto roles
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				result.Roles.AddRange((await provider.ListRoles(site)).Where(role => role != site.RegisteredUsersRole && role.Type.HasFlag(Role.RoleType.AutoAssign)));
			}			

			// add user profile properties for the site
			foreach (UserProfileProperty property in site.UserProfileProperties)
			{
				result.Profile.Add(new UserProfileValue() { UserProfileProperty = property });
			}

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<User> Get(Guid id)
		{
			User result = await this.CacheManager.UserCache().GetAsync(id, async id =>
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					return await provider.GetUser(id);
				}
			});

			if ( result != null)
			{
				Site site = null;

				// System administrators don't have a site
				if (result.SiteId.HasValue)
				{
					using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
					{
						site = await provider.GetSite(result.SiteId.Value);
					}
				}
				
				if (site != null && result != null)
				{
					foreach (UserProfileProperty property in site.UserProfileProperties)
					{
						if (!result.Profile.Where(profilevalue => profilevalue.UserProfileProperty.Id == property.Id).Any())
						{
							result.Profile.Add(new UserProfileValue() { UserProfileProperty = property });
						}
					}

					result.Profile = result.Profile.OrderBy(profileValue => profileValue.UserProfileProperty.SortOrder).ToList();
				}
			}

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<User> Get(Site site, Guid id)
		{
			User result = await this.CacheManager.UserCache().GetAsync(id, async id =>
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					return await provider.GetUser(id);
				}
			});

			if (site != null && result != null)
			{
				foreach (UserProfileProperty property in site.UserProfileProperties)
				{
					if (!result.Profile.Where(profilevalue => profilevalue.UserProfileProperty.Id == property.Id).Any())
					{
						result.Profile.Add(new UserProfileValue() { UserProfileProperty = property });
					}
				}

				result.Profile = result.Profile.OrderBy(profileValue => profileValue.UserProfileProperty.SortOrder).ToList();
			}
			
			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database, matching by the specified userName.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<User> Get(Site site, string userName)
		{
			// This function is only used by the login and change password pages so it doesn't try to read from the cache
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.GetUserByName(site, userName);
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database, matching by the specified userName.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<User> GetByEmail(Site site, string email)
		{
			// This function is only used by the password recovery module so it doesn't try to read from the cache
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider .GetUserByEmail(site, email);
			}
		}


		/// <summary>
		/// Retrieve an existing System Administrator <see cref="User"/> from the database, matching by the specified userName.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<User> GetSystemAdministrator(string userName)
		{			
			// This function is only used by the login and change password pages so it doesn't try to read from the cache
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.GetSystemAdministrator(userName);
			}
		}

		/// <summary>
		/// Add the specified <see cref="Role"/> to the specified <see cref="User"/>.  
		/// </summary>
		/// <param name="user"></param>
		/// <param name="roleId"></param>
		/// <remarks>
		/// Changes are not saved to the database.  Call the <see cref="Save(Site, User)"/> method to save changes.
		/// </remarks>
		public async Task AddRole(User user, Guid roleId)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				if (user.Roles==null)
				{
					user.Roles = new();
				}

				user.Roles.Add(await provider.GetRole(roleId));
			}
		}

		/// <summary>
		/// Removes the specified <see cref="Role"/> to the specified <see cref="User"/>.  
		/// </summary>
		/// <param name="user"></param>
		/// <param name="roleId"></param>
		/// <remarks>
		/// Changes are not saved to the database.  Call the <see cref="Save(Site, User)"/> method to save changes.
		/// </remarks>
		public async Task RemoveRole(User user, Guid roleId)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				user.Roles.Remove(await provider.GetRole(roleId));
			}			
		}

		/// <summary>
		/// List all <see cref="User"/>s who belong to the specified <see cref="Site"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IList<User>> List(Site site)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListUsers(site);
			}
		}


		/// <summary>
		/// List a page of <see cref="User"/>s who belong to the specified <see cref="Site"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListUsers(site, pagingSettings);
			}
		}

		/// <summary>
		/// List all <see cref="User"/> who belong to any of the specified <see cref="Role"/>s.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IList<User>> ListUsersInRole(Role role)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListUsersInRole(role.Id);
			}
		}

		/// <summary>
		/// List the <see cref="User"/>s who are members of the specified <see cref="Role"/> with paging.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListUsersInRole(Role role, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListUsersInRole(role.Id, pagingSettings);
			}
		}

		/// <summary>
		/// List all System Administrator <see cref="User"/>s.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> ListSystemAdministrators(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListSystemAdministrators(pagingSettings);
			}
		}

		/// <summary>
		/// Count the System Administrator <see cref="User"/>s.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<long> CountSystemAdministrators()
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.CountSystemAdministrators();
			}
		}

		/// <summary>
		/// Returns a list of <see cref="User"/>s whi match the specified searchTerm.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="searchTerm"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<User>> Search(Site site, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.SearchUsers(site, searchTerm, pagingSettings);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="User"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="user"></param>
		public async Task Save(Site site, User user)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				await provider.SaveUser(site, user);
				this.CacheManager.UserCache().Remove(user.Id);
			}
		}

		public void SetNewUserFlags(Site site, User user)
		{
			user.Approved = !site.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.RequireApproval);
			user.Verified = !site.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.RequireEmailVerification);
		}

		public async Task SaveSecrets(User user)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
			  await provider.SaveUserSecrets(user);
				this.CacheManager.UserCache().Remove(user.Id);
			}
		}

		/// <summary>
		/// Create or update the specified System Administrator <see cref="User"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="user"></param>
		public async Task SaveSystemAdministrator(User user)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				await provider .SaveSystemAdministrator(user);
				this.CacheManager.UserCache().Remove(user.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="User"/> from the database.
		/// </summary>
		/// <param name="user"></param>
		public async Task Delete(User user)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				await provider .DeleteUser(user);
				this.CacheManager.UserCache().Remove(user.Id);
			}
		}

	}
}
