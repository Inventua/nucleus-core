using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Class used to retrieve resource options from configuration files.
	/// </summary>
	public class ResourceFileOptions
	{
		/// <summary>
		/// Configuration file section key
		/// </summary>
		public const string Section = "Nucleus:ResourceFileOptions";

		/// <summary>
		/// Specifies whether to use minified javascript files, if available.
		/// </summary>
		public Boolean UseMinifiedJs { get; private set; } = true;

		/// <summary>
		/// Specifies whether to use minified css files, if available.
		/// </summary>
		public Boolean UseMinifiedCss { get; private set; } = true;

		/// <value>
		/// Specifies whether to merge requests for Javascript files.
		/// </value>
		public Boolean MergeJs { get; private set; } = false;

		/// <value>
		/// Specifies whether to merge requests for CSS files.
		/// </value>
		public Boolean MergeCss { get; private set; } = false;
	}
}
