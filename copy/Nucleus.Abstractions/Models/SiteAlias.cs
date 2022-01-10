using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models
{
	public class SiteAlias
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the Site Alias.
		/// </summary>
		/// <remarks>
		/// The site alias is used to identify the site from a HTTP request.  The site alias is a domain name and optionally includes
		/// a PathBase, which is only available when using IIS, and is a virtual directory name.
		/// </remarks>
		[Required]
		public string Alias { get; set; }
		

	}
}
