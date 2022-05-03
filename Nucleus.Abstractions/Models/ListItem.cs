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
	public class ListItem : ModelBase, IComparable
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Item Name
		/// </summary>
		/// <remarks>
		/// </remarks>		
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// List item value
		/// </summary>
		/// <remarks>
		/// The site to which the list belongs.  A null value represents a instance-wide list.
		/// </remarks>
		public string Value { get; set; }

		/// <summary>
		/// Compare one list item to another.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method implements the IComparable interface and is used for sorting.
		/// </remarks>
		public int CompareTo(object other)
		{
			ListItem otherListItem = other as ListItem;
			return this.Name.CompareTo(otherListItem.Name);
		}

		/// <summary>
		///	Return the list item value.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
    {
			return this.Value;
    }
	}
}
