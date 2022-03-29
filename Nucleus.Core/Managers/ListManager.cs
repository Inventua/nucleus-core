using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="List"/>s.
	/// </summary>
	public class ListManager : IListManager
	{
		private ICacheManager CacheManager { get; }
		private IDataProviderFactory DataProviderFactory { get; }

		public ListManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
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
		public Task<List> CreateNew()
		{
			return Task.FromResult(new List());
		}

		/// <summary>
		/// Retrieve an existing <see cref="List"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<List> Get(Guid id)
		{
			return await this.CacheManager.ListCache().GetAsync(id, async id =>
			{
				using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
				{
					return await provider.GetList(id);
				}
			});
		}

		/// <summary>
		/// Retrieve an existing <see cref="List"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ListItem> GetListItem(Guid id)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{
				return await provider.GetListItem(id);
			}
			
		}

		/// <summary>
		/// List all <see cref="List"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IEnumerable<List>> List(Site site)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{
				return await provider.ListLists(site);
			}
		}


		/// <summary>
		/// List paged <see cref="List"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<List>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{
				return await provider.ListLists(site, pagingSettings);
			}
		}


		/// <summary>
		/// Create or update the specified <see cref="List"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="list"></param>
		public async Task Save(Site site, List list)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{				
				await provider.SaveList(site, list);
				this.CacheManager.ListCache().Remove(list.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="List"/> from the database.
		/// </summary>
		/// <param name="list"></param>
		public async Task Delete(List list)
		{
			using (IListDataProvider provider = this.DataProviderFactory.CreateProvider<IListDataProvider>())
			{
				await provider.DeleteList(list);
				this.CacheManager.ListCache().Remove(list.Id);
			}
		}

	}
}
