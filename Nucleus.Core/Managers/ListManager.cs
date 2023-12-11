using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core.Managers;

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
		if (id == Guid.Empty) return default;

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

      if (list.Items.Where(item => item.SortOrder == null).Any())
      {
        await CheckNumbering(site, list);
      }

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

	/// <summary>
	/// Update the <see cref="ListItem.SortOrder"/> of the Lists specifed by id by swapping it with the next-highest <see cref="ListItem.SortOrder"/>.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="list"></param>
	/// <param name="itemId"></param>
	public async Task MoveDown(Site site, List list, Guid itemId)
	{
		await MoveSortOrder(site, list, itemId, true);
	}

	/// <summary>
	/// Update the <see cref="ListItem.SortOrder"/> of the ListItem specifed by <paramref name="list"/> and <paramref name="itemId"/> 
	/// swapping it with the previous <see cref="ListItem.SortOrder"/>.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="list"></param>
	/// <param name="itemId"></param>
	public async Task MoveUp(Site site, List list, Guid itemId)
	{
		await MoveSortOrder(site, list, itemId, false);
	}

	/// <summary>
	/// Update the <see cref="ListItem.SortOrder"/> of the ListItem specifed by <paramref name="list"/> and <paramref name="itemId"/> 
	/// by swapping it with the previous <see cref="ListItem.SortOrder"/>.
	/// </summary>
	/// <param name="forumId"></param>
	private async Task MoveSortOrder(Site site, List list, Guid itemId, Boolean moveDown)
	{
		ListItem previousItem = null;

		// Copy the list (enumerator) because if this is a moveDown we reverse the order and don't want to affect other users.
		List<ListItem> listItems = moveDown ? list.Items.Reverse<ListItem>().ToList() : list.Items.ToList();

		await CheckNumbering(site, list);

		foreach (ListItem listItem in listItems)
		{
			if (listItem.Id == itemId)
			{
				if (previousItem != null)
				{
					int? temp = listItem.SortOrder;
					listItem.SortOrder = previousItem.SortOrder;
					previousItem.SortOrder = temp;

					if (previousItem.SortOrder == listItem.SortOrder)
					{
						previousItem.SortOrder += 10;
					}
				}
			}
			else
			{
				previousItem = listItem;
			}
		}

		// Update the new list items in list
		list.Items = listItems.OrderBy(item => item.SortOrder).ToList(); //listItems;

		// Call Save() to save to the database (which will also remove from cache)
		await this.Save(site, list);
		this.CacheManager.ListCache().Remove(list.Id);
	}


	/// <summary>
	/// Ensure that list items have unique sort order.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="list"></param>
	/// <remarks>
	/// List item sort orders can produce duplicates and gaps when list item parents are changed, or list items are deleted.
	/// </remarks>
	private async Task CheckNumbering(Site site, List list)
	{
		int sortOrder = 10;

		foreach (ListItem listItem in list.Items)
		{
			if (listItem.SortOrder != sortOrder)
			{
				listItem.SortOrder = sortOrder;
			}

			sortOrder += 10;
		}

		list.Items = list.Items.OrderBy(item => item.SortOrder).ToList();
		await this.Save(site, list);
	}
}
