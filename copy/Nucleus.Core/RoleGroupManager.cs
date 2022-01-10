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
	/// Provides functions to manage database data for <see cref="RoleGroup"/>s.
	/// </summary>
	public class RoleGroupManager
	{
		
		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }

		public RoleGroupManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
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
		public RoleGroup CreateNew()
		{
			return new RoleGroup();
		}

		/// <summary>
		/// Retrieve an existing <see cref="RoleGroup"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public RoleGroup Get(Guid id)
		{
			return this.CacheManager.RoleGroupCache.Get(id, id =>
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					return  provider.GetRoleGroup(id);					
				}
			});
		}

		/// <summary>
		/// List all <see cref="RoleGroup"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IEnumerable<RoleGroup> List(Site site)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				return provider.ListRoleGroups(site);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="RoleGroup"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="roleGroup"></param>
		public void Save(Site site, RoleGroup roleGroup)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				provider.SaveRoleGroup(site, roleGroup);
				this.CacheManager.RoleGroupCache.Remove(roleGroup.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="RoleGroup"/> from the database.
		/// </summary>
		/// <param name="roleGroup"></param>
		public void Delete(RoleGroup roleGroup)
		{
			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				provider.DeleteRoleGroup(roleGroup);
			}
		}
	}
}
