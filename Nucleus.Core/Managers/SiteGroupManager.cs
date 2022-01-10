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
	/// Provides functions to manage database data for <see cref="SiteGroup"/>s.
	/// </summary>
	public class SiteGroupManager : ISiteGroupManager
	{		
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public SiteGroupManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="SiteGroup"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="SiteGroup"/> to the database.  Call <see cref="Save(Site, SiteGroup)"/> to save the Site group.
		/// </remarks>
		public Task<SiteGroup> CreateNew()
		{
			return Task.FromResult(new SiteGroup());
		}

		/// <summary>
		/// Retrieve an existing <see cref="SiteGroup"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<SiteGroup> Get(Guid id)
		{
			return await this.CacheManager.SiteGroupCache().GetAsync(id, async id =>
			{
				using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
				{
					return await provider.GetSiteGroup(id);					
				}
			});
		}

		/// <summary>
		/// List all <see cref="SiteGroup"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IEnumerable<SiteGroup>> List()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return await provider.ListSiteGroups();
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="SiteGroup"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="SiteGroup"></param>
		public async Task Save(SiteGroup siteGroup)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.SaveSiteGroup(siteGroup);
				this.CacheManager.SiteGroupCache().Remove(siteGroup.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="SiteGroup"/> from the database.
		/// </summary>
		/// <param name="SiteGroup"></param>
		public async Task Delete(SiteGroup siteGroup)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				await provider.DeleteSiteGroup(siteGroup);
			}
		}
	}
}
