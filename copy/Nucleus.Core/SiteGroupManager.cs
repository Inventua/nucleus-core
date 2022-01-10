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
	/// Provides functions to manage database data for <see cref="SiteGroup"/>s.
	/// </summary>
	public class SiteGroupManager
	{
		
		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }

		public SiteGroupManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
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
		public SiteGroup CreateNew()
		{
			return new SiteGroup();
		}

		/// <summary>
		/// Retrieve an existing <see cref="SiteGroup"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public SiteGroup Get(Guid id)
		{
			return this.CacheManager.SiteGroupCache.Get(id, id =>
			{
				using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
				{
					return  provider.GetSiteGroup(id);					
				}
			});
		}

		/// <summary>
		/// List all <see cref="SiteGroup"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IEnumerable<SiteGroup> List()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.ListSiteGroups();
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="SiteGroup"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="SiteGroup"></param>
		public void Save(SiteGroup siteGroup)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.SaveSiteGroup(siteGroup);
				this.CacheManager.SiteGroupCache.Remove(siteGroup.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="SiteGroup"/> from the database.
		/// </summary>
		/// <param name="SiteGroup"></param>
		public void Delete(SiteGroup siteGroup)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				provider.DeleteSiteGroup(siteGroup);
			}
		}
	}
}
