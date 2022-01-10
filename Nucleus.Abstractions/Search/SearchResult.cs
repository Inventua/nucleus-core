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
	public class SearchResult : ContentMetaData
	{

		/// <summary>
		/// Search result score.
		/// </summary>
		public double? Score { get; set; }

	}
}
