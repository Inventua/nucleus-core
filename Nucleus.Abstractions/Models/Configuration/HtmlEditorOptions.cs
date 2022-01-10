using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents Html editor options from configuration.
	/// </summary>
	public class HtmlEditorOptions
	{
		/// <summary>
		/// Configuration file section path for Html Editor options.
		/// </summary>
		public const string Section = "Nucleus:HtmlEditor";

		/// <summary>
		/// Html editor scripts (css and javascript), read from configuration.
		/// </summary>
		public List<HtmlEditorScript> Scripts { get; private set; } = new();
	}
}
