using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Search
{
	/// <summary>
	/// Represents a search result.
	/// </summary>
	/// <remarks>
	/// Refer to <seealso cref="ContentMetaData"/> for more information.
	/// </remarks>
	public class SearchResult : ContentMetaData
	{

		/// <summary>
		/// Search result score.
		/// </summary>
		public double? Score { get; set; }

	}
}
