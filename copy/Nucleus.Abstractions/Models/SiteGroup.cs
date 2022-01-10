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
	public class SiteGroup
	{
		public Guid Id { get; set; }

		[Required]
		public string Name { get; set; }

		public string Description { get; set; }

		public Site PrimarySite { get; set; }
	}
}
