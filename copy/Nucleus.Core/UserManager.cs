using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="User"/>s.
	/// </summary>
	public class UserManager
	{
		private DataProviderFactory DataProviderFactory { get; }
		private PasswordOptions PasswordOptions { get; }
		private ClaimTypeOptions ClaimTypeOptions { get; }
		private CacheManager CacheManager { get; }
		private ILogger<UserManager> Logger { get; }

		// todo: make these configurable
		//public const int FAILED_PASSWORD_WINDOW_TIMEOUT_MINUTES = 15;
		//public const int FAILED_PASSWORD_MAX_ATTEMPTS = 3;
		//public const int LOCKOUT_RESET_MINUTES = 10;
		//public const int RESET_TOKEN_EXPIRY_MINUTES_DEFAULT = 120;

		public UserManager(ILogger<UserManager> logger, DataProviderFactory dataProviderFactory, CacheManager cacheManager, IOptions<PasswordOptions> passwordOptions, IOptions<ClaimTypeOptions> claimTypeOptions)
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
		/// <param name="password"></param>
		/// <returns></returns>
		/// <remarks>
		/// Each <see cref="PasswordComplexityRule"/> is executed, and the results are returned in a ModelStateDictionary.
		/// </remarks>
		public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ValidatePasswordComplexity(string password)
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
							modelState.AddModelError(rule.Pattern, rule.Message);
						}
					}
					catch (Exception e)
					{
						modelState.AddModelError(rule.Pattern, $"Unable to process password complexity rule '{rule?.Message}': {e.Message}");
					}
				}
			}

			return modelState;
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
		public Boolean VerifyPassword(User user, string password)
		{
			if (user.Secrets.IsLockedOut)
			{
				//if (user.Secrets.LastLockoutDate < DateTime.UtcNow.AddMinutes(-LOCKOUT_RESET_MINUTES))
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

			if (String.IsNullOrEmpty(password) || user.Secrets == null || !user.Secrets.VerifyPassword(password))
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
					provider.SaveUserSecrets(user);
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

				user.Secrets.FailedPasswordWindowStart = DateTime.MinValue;
				user.Secrets.FailedPasswordAttemptCount = 0;
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					provider.SaveUserSecrets(user);
				}

				return true;
			}
		}

		public void SetPasswordResetToken(User user)
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
				provider.SaveUserSecrets(user);
			}
		}

		/// <summary>
		/// Create a new <see cref="User"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="User"/> to the database.  Call <see cref="Save(Site, User)"/> to save the role group.
		/// </remarks>
		public User CreateNew(Site site)
		{
			User result = new();
			result.Roles = new();

			// add new user to registered users role
			if (site.RegisteredUsersRole != null)
			{
				result.Roles.Add(site.RegisteredUsersRole);
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
		public User Get(Site site, Guid id)
		{
			User result = this.CacheManager.UserCache.Get(id, id =>
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					return provider.GetUser(id);
				}
			});

			if (site != null && result != null)
			{
				foreach (UserProfileProperty property in site.UserProfileProperties)
				{
					if (!result.Profile.Where(profile => profile.UserProfileProperty.Id == property.Id).Any())
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
		public User Get(Site site, string userName)
		{
			// This function is only used by the login and change password pages so it doesn't try to read from the cache
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return provider.GetUserByName(site, userName);
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="User"/> from the database, matching by the specified userName.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public User GetByEmail(Site site, string email)
		{
			// This function is only used by the password recovery module so it doesn't try to read from the cache
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return provider.GetUserByEmail(site, email);
			}
		}


		/// <summary>
		/// Retrieve an existing System Administrator <see cref="User"/> from the database, matching by the specified userName.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public User GetSystemAdministrator(string userName)
		{			
			// This function is only used by the login and change password pages so it doesn't try to read from the cache
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return provider.GetSystemAdministrator(userName);
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
		public void AddRole(User user, Guid roleId)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				if (user.Roles==null)
				{
					user.Roles = new();
				}

				user.Roles.Add(provider.GetRole(roleId));
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
		public void RemoveRole(User user, Guid roleId)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				user.Roles.Remove(provider.GetRole(roleId));
			}
		}

		/// <summary>
		/// List all <see cref="User"/>s who belong to the specified <see cref="Site"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IEnumerable<User> List(Site site)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return provider.ListUsers(site);
			}
		}

		/// <summary>
		/// List all System Administrator <see cref="User"/>s.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IEnumerable<User> ListSystemAdministrators()
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return provider.ListSystemAdministrators();
			}
		}

		/// <summary>
		/// Returns a list of <see cref="User"/>s whi match the specified searchTerm.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="searchTerm"></param>
		/// <returns></returns>
		public IEnumerable<User> Search(Site site, string searchTerm)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return provider.SearchUsers(site, searchTerm);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="User"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="user"></param>
		public void Save(Site site, User user)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				provider.SaveUser(site, user);
				this.CacheManager.UserCache.Remove(user.Id);
			}
		}

		/// <summary>
		/// Create or update the specified System Administrator <see cref="User"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="user"></param>
		public void SaveSystemAdministrator(User user)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				provider.SaveSystemAdministrator(user);
				this.CacheManager.UserCache.Remove(user.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="User"/> from the database.
		/// </summary>
		/// <param name="user"></param>
		public void Delete(User user)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				provider.DeleteUser(user);
				this.CacheManager.UserCache.Remove(user.Id);
			}
		}

	}
}
