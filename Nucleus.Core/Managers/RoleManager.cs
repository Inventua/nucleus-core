using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Role"/>s.
	/// </summary>
	public class RoleManager : IRoleManager
	{
		private ICacheManager CacheManager { get; }
		private IDataProviderFactory DataProviderFactory { get; }

		public RoleManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
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
		public Task<Role> CreateNew()
		{
			return Task.FromResult(new Role());
		}

		/// <summary>
		/// Retrieve an existing <see cref="Role"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Role> Get(Guid id)
		{
			return await this.CacheManager.RoleCache().GetAsync(id, async id =>
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					return await provider.GetRole(id);
				}
			});
		}

		/// <summary>
		/// Retrieve an existing <see cref="Role"/> from the database.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public async Task<Role> GetByName(Site site, string name)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.GetRoleByName(name);
			};
		}

		/// <summary>
		/// List all <see cref="Role"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IEnumerable<Role>> List(Site site)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListRoles(site);
			}
		}

		/// <summary>
		/// List paged <see cref="Role"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<Role>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListRoles(site, pagingSettings);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="role"></param>
		public async Task Save(Site site, Role role)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{				
				await provider.SaveRole(site, role);
				this.CacheManager.RoleCache().Remove(role.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="Role"/> from the database.
		/// </summary>
		/// <param name="role"></param>
		public async Task Delete(Role role)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				await provider.DeleteRole(role);
				this.CacheManager.RoleCache().Remove(role.Id);
			}
		}

	}
}
