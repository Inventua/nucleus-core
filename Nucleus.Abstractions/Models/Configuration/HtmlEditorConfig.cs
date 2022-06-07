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
	public class HtmlEditorConfig
	{
		/// <summary>
		/// Gets a value that indicates the relative script path.
		/// </summary>
		public string Key { get; private set; }

		/// <summary>
		/// Html editor scripts (css and javascript), read from configuration.
		/// </summary>
		public List<HtmlEditorScript> Scripts { get; private set; } = new();
	}
}
