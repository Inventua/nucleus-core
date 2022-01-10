using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represent a site group.
	/// </summary>
	/// <remarks>
	/// Site groups are for future use.
	/// </remarks>
	public class SiteGroup : ModelBase
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Site group name.
		/// </summary>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Site group description.  Descriptions are shown in the admin user interface.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Specifies the primary site for the site group.  When a site is added to a site group, users, roles and other entities from the
		/// primary site are used by that site instead of an individual set for the site.
		/// </summary>
		public Site PrimarySite { get; set; }
	}
}
