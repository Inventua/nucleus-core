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
	/// Provides functions to manage database data for <see cref="List"/>s.
	/// </summary>
	public class ListManager
	{
		private CacheManager CacheManager { get; }
		private DataProviderFactory DataProviderFactory { get; }

		public ListManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="List"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="List"/> to the database.  Call <see cref="Save(Site, List)"/> to save the list.
		/// </remarks>
		public List CreateNew()
		{
			return new List();
		}

		/// <summary>
		/// Retrieve an existing <see cref="List"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public List Get(Guid id)
		{
			return this.CacheManager.ListCache.Get(id, id =>
			{
				using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
				{
					return provider.GetList(id);
				}
			});
		}

		/// <summary>
		/// Retrieve an existing <see cref="List"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ListItem GetListItem(Guid id)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{
				return provider.GetListItem(id);
			}
			
		}

		/// <summary>
		/// List all <see cref="List"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IList<List> List(Site site)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{
				return provider.ListLists(site);
			}
		}


		/// <summary>
		/// Create or update the specified <see cref="List"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="list"></param>
		public void Save(Site site, List list)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{				
				provider.SaveList(site, list);
				this.CacheManager.ListCache.Remove(list.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="List"/> from the database.
		/// </summary>
		/// <param name="list"></param>
		public void Delete(List list)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{
				provider.DeleteList(list);
				this.CacheManager.ListCache.Remove(list.Id);
			}
		}

	}
}
