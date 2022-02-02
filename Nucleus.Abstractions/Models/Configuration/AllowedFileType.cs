using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents the configurable options for a file type signature.
	/// </summary>
	/// <remarks>
	/// File type signatures are configured in appSettings.json and are used to check the integrity of uploaded files.
	/// </remarks>
	/// <example>
	/// "AllowedFileTypes": {
	///   "JPEG": { "FileExtensions": [".jpg", ".jpeg"], "Signatures": [ "FFD8FFE0", "FFD8FFE2", "FFD8FFE3" ] }
	///  }
	///</example>
	public class AllowedFileType
	{
		/// <summary>
		/// List of allowed file extensions for the allowed file type.
		/// </summary>
		/// <remarks>
		/// Some file types can have multiple file extensions. 
		/// </remarks>
		public List<string> FileExtensions { get; private set; }

		/// <summary>
		/// A list of signatures for the file type.  Signatures are used to validate that the file contents match the file type (extension).
		/// </summary>
		/// <remarks>
		/// During upload, files integrity is validated by comparing the first few bytes of the file with the specified signatures.  The file bytes 
		/// must match at least one of the signatures.  Signatures are specified as hexadecimal values, with no spaces or delimiters.  The special 
		/// value "??" in a signature skips validation of the byte in the ordinal position represented by the ?? characters.
		/// </remarks>
		public List<string> Signatures { get; private set; }

		/// <summary>
		/// Specifies whether uploads if the file type are restricted to site administrators.
		/// </summary>
		public Boolean Restricted { get; private set; }

	}
}
