using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents a Html editor script (javascript, css) defined in configuration.
	/// </summary>
	public class HtmlEditorScript
	{
		/// <summary>
		/// Script type
		/// </summary>
		public enum Types
		{
			/// <summary>
			/// Css
			/// </summary>
			stylesheet,
			/// <summary>
			/// Javascript
			/// </summary>
			javascript
		}

		/// <summary>
		/// Gets a value that indicates the script type.
		/// </summary>
		public Types Type { get; private set; }
		/// <summary>
		/// Gets a value that indicates the relative script path.
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Gets a value that indicates that the script does dynamic loading, and should be excluded from being included in script merging.
		/// </summary>
		public Boolean IsDynamic { get; private set; }
	}
}
