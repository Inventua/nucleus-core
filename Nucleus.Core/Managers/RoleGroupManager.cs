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
	/// Provides functions to manage database data for <see cref="RoleGroup"/>s.
	/// </summary>
	public class RoleGroupManager : IRoleGroupManager
	{		
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public RoleGroupManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="RoleGroup"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="RoleGroup"/> to the database.  Call <see cref="Save(Site, RoleGroup)"/> to save the role group.
		/// </remarks>
		public Task<RoleGroup> CreateNew()
		{
			return Task.FromResult(new RoleGroup());
		}

		/// <summary>
		/// Retrieve an existing <see cref="RoleGroup"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<RoleGroup> Get(Guid id)
		{
			return await this.CacheManager.RoleGroupCache().GetAsync(id, async id =>
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					return await provider.GetRoleGroup(id);					
				}
			});
		}

    /// <summary>
		/// Retrieve an existing <see cref="RoleGroup"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<RoleGroup> GetByName(Site site, string name)
    {
      using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
      {
        return await provider.GetRoleGroupByName(site, name);
      }     
    }

    /// <summary>
    /// List all <see cref="RoleGroup"/>s for the specified site.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    public async Task<IEnumerable<RoleGroup>> List(Site site)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListRoleGroups(site);
			}
		}

		/// <summary>
		/// List paged <see cref="RoleGroup"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<RoleGroup>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return await provider.ListRoleGroups(site, pagingSettings);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="RoleGroup"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="roleGroup"></param>
		public async Task Save(Site site, RoleGroup roleGroup)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				await provider.SaveRoleGroup(site, roleGroup);
				this.CacheManager.RoleGroupCache().Remove(roleGroup.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="RoleGroup"/> from the database.
		/// </summary>
		/// <param name="roleGroup"></param>
		public async Task Delete(RoleGroup roleGroup)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				await provider.DeleteRoleGroup(roleGroup);
			}
		}
	}
}
