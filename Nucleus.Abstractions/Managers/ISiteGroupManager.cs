using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="SiteGroup"/>s.
	/// </summary>
	public interface ISiteGroupManager
	{
		/// <summary>
		/// Create a new <see cref="SiteGroup"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="SiteGroup"/> to the database.  Call <see cref="Save(SiteGroup)"/> to save the Site group.
		/// </remarks>
		public Task<SiteGroup> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="SiteGroup"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<SiteGroup> Get(Guid id);

		/// <summary>
		/// List all <see cref="SiteGroup"/>s for the specified site.
		/// </summary>
		/// <returns></returns>
		public Task<IEnumerable<SiteGroup>> List();

		/// <summary>
		/// Create or update the specified <see cref="SiteGroup"/>.
		/// </summary>
		/// <param name="siteGroup"></param>
		public Task Save(SiteGroup siteGroup);

		/// <summary>
		/// Delete the specified <see cref="SiteGroup"/> from the database.
		/// </summary>
		/// <param name="siteGroup"></param>
		public Task Delete(SiteGroup siteGroup);
	}
}
