using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.ComponentModel;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Class used to retrieve options for the MinifiedFileProvider.  
	/// </summary>
	/// <remarks>
	/// The properties of the MinifiedFileProviderOptions class control whether the MinifiedFileProvider is enabled.
	/// </remarks>
	public class MinifiedFileProviderOptions
	{
		/// <summary>
		/// Configuration section
		/// </summary>
		public const string Section = "Nucleus:MinifiedFileProviderOptions";

		/// <value>
		/// Specifies whether to minify Javascript files.
		/// </value>
		public Boolean MinifyJs { get; set; } = false;

		/// <value>
		/// Specifies whether to minify CSS files.
		/// </value>
		public Boolean MinifyCss { get; set; } = false;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public MinifiedFileProviderOptions()
		{

		}


	}
}
