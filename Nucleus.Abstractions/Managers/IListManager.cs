using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="List"/>s.
	/// </summary>
	public interface IListManager
	{
		/// <summary>
		/// Create a new <see cref="List"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="List"/> to the database.  Call <see cref="Save(Site, List)"/> to save the list.
		/// </remarks>
		public Task<List> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="List"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<List> Get(Guid id);

		/// <summary>
		/// Retrieve an existing <see cref="List"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<ListItem> GetListItem(Guid id);

		/// <summary>
		/// List all <see cref="List"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public Task<IList<List>> List(Site site);

		/// <summary>
		/// Create or update the specified <see cref="List"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="list"></param>
		public Task Save(Site site, List list);

		/// <summary>
		/// Delete the specified <see cref="List"/> from the database.
		/// </summary>
		/// <param name="list"></param>
		public Task Delete(List list);

	}
}
