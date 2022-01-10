//using Microsoft.Extensions.FileProviders;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using System.ComponentModel;

//namespace Nucleus.Extensions.FileProviders
//{
//	/// <summary>
//	/// Class used to retrieve options for the MergedFileProvider.  
//	/// </summary>
//	/// <remarks>
//	/// The properties of the MergedFileProviderOptions class control whether the MergedScriptsTagHelper 
//	/// and MergedStyleSheetsTagHelper are enabled.
//	/// </remarks>
//	public class MergedFileProviderOptions
//	{
//		internal const string Section = "MergedFileProviderOptions";

//		/// <value>
//		/// Specifies whether to merge requests for Javascript files.
//		/// </value>
//		public Boolean MergeJs { get; set; } = false;

//		/// <value>
//		/// Specifies whether to merge requests for CSS files.
//		/// </value>
//		public Boolean MergeCss { get; set; } = false;

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public MergedFileProviderOptions()
//		{

//		}


//	}
//}
