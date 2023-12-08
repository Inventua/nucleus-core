using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Nucleus.Abstractions.Models.List"/>s.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IListManager
	{
		/// <summary>
		/// Create a new <see cref="Nucleus.Abstractions.Models.List"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="Nucleus.Abstractions.Models.List"/> to the database.  Call <see cref="Save(Site, List)"/> to save the list.
		/// </remarks>
		public Task<List> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="Nucleus.Abstractions.Models.List"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<List> Get(Guid id);

		/// <summary>
		/// Retrieve an existing <see cref="Nucleus.Abstractions.Models.List"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<ListItem> GetListItem(Guid id);

		/// <summary>
		/// List all <see cref="Nucleus.Abstractions.Models.List"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public Task<IEnumerable<List>> List(Site site);

		/// <summary>
		/// List paged <see cref="Nucleus.Abstractions.Models.List"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="pagingSettings"></param>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<List>> List(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Create or update the specified <see cref="Nucleus.Abstractions.Models.List"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="list"></param>
		public Task Save(Site site, List list);

		/// <summary>
		/// Delete the specified <see cref="Nucleus.Abstractions.Models.List"/> from the database.
		/// </summary>
		/// <param name="list"></param>
		public Task Delete(List list);

		/// <summary>
		/// Move list item down 
		/// </summary>
		/// <param name="site"></param>
		/// <param name="list"></param>
		/// <param name="itemId"></param>
		public Task MoveDown(Site site, List list, Guid itemId);

		/// <summary>
		/// Move list item up 
		/// </summary>
		/// <param name="site"></param>
		/// <param name="list"></param>
		/// <param name="itemId"></param>
		public Task MoveUp(Site site, List list, Guid itemId);
	}
}
