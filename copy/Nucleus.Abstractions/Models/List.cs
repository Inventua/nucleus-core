using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents a general-use list of items.  
	/// </summary> 
	/// <remarks>
	/// </remarks>
	public class List 
	{
	
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }
			
		/// <summary>
		/// List Name
		/// </summary>
		/// <remarks>
		/// </remarks>		
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Description
		/// </summary>
		/// <remarks>
		/// Descriptions are for reference purposes only.  They are displayed in the administrative interface.
		/// </remarks>
		public string Description { get; set; }

		/// <summary>
		/// Items in the list
		/// </summary>
		public List<ListItem> Items { get; set; }
	}
}
