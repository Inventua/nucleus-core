using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Role"/>s.
	/// </summary>
	public class RoleManager
	{
		private CacheManager CacheManager { get; }
		private DataProviderFactory DataProviderFactory { get; }

		public RoleManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="Role"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="Role"/> to the database.  Call <see cref="Save(Site, Role)"/> to save the role.
		/// </remarks>
		public Role CreateNew()
		{
			return new Role();
		}

		/// <summary>
		/// Retrieve an existing <see cref="Role"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Role Get(Guid id)
		{
			return this.CacheManager.RoleCache.Get(id, id =>
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					return provider.GetRole(id);
				}
			});
		}

		/// <summary>
		/// List all <see cref="Role"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IEnumerable<Role> List(Site site)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return provider.ListRoles(site);
			}
		}


		/// <summary>
		/// Create or update the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="role"></param>
		public void Save(Site site, Role role)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{				
				provider.SaveRole(site, role);
				this.CacheManager.RoleCache.Remove(role.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="Role"/> from the database.
		/// </summary>
		/// <param name="role"></param>
		public void Delete(Role role)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				provider.DeleteRole(role);
				this.CacheManager.RoleCache.Remove(role.Id);
			}
		}

	}
}
