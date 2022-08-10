using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.ViewFeatures.ViewModels
{
	/// <summary>
	///  View model for PagingControl
	/// </summary>
	public class PagingControl
	{
		/// <summary>
		/// Enum representing control render mode.
		/// </summary>
		/// <type>Enum</type>
		public enum RenderModes
		{
			/// <value>
			/// Render all controls
			/// </value>
			Standard,
			/// <value>
			/// Render the control without the individual paging and page size buttons
			/// </value>
			Compact
		}


		/// <summary>
		/// Selected paging information property name in the caller's view model.
		/// </summary>
		/// <remarks>
		/// The specified property should be of type <see cref="PagingSettings"/>
		/// </remarks>
		public string PropertyName { get; set; }

		/// <summary>
		/// Current paging settings selection
		/// </summary>
		public PagingSettings Results { get; set; }

		/// <summary>
		/// Control rendering mode.
		/// </summary>
		public RenderModes RenderMode { get; set; }
	}
}
