using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// This class is used to cache page route matches, including when a page route partly matched a route - along 
	/// with the LocalPath (non-matching part of the path) for later use.
	/// </summary>
	/// <internal></internal>
	public class FoundPage
	{
		/// <summary>
		/// Represents the matched page
		/// </summary>
		public Page Page { get; init; }

		/// <summary>
		/// Represents the full matched path
		/// </summary>
		public string RequestPath { get; set; }

		/// <summary>
		/// Represents the 
		/// </summary>
		public LocalPath LocalPath { get; init; } = new("");
	}
}
