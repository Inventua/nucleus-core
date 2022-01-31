using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.ComponentModel;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Class used to retrieve options for the MergedFileProvider.  
	/// </summary>
	/// <remarks>
	/// The properties of the MergedFileProviderOptions class control whether the MergedScriptsTagHelper 
	/// and MergedStyleSheetsTagHelper are enabled.
	/// </remarks>
	public class MergedFileProviderOptions
	{
		/// <summary>
		/// Configuration section
		/// </summary>
		public const string Section = "Nucleus:MergedFileProviderOptions";

		/// <summary>
		/// Delimiter for merged filenames.
		/// </summary>
		public const string SEPARATOR_CHAR = ",";

		/// <value>
		/// Specifies whether to merge requests for Javascript files.
		/// </value>
		public Boolean MergeJs { get; set; } = false;

		/// <value>
		/// Specifies whether to merge requests for CSS files.
		/// </value>
		public Boolean MergeCss { get; set; } = false;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public MergedFileProviderOptions()
		{

		}


	}
}
