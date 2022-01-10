using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.FileSystem
{
	/// <summary>
	/// Rule used to validate a file system item name.
	/// </summary>
	public class FileSystemValidationRule
	{
		/// <summary>
		/// Specifies a regular expression used to validate a file system ite. name.
		/// </summary>
		public string ValidationExpression { get; set; }

		/// <summary>
		/// Specifies an error message to display if the validation rule fails.
		/// </summary>
		public string ErrorMessage { get; set; }
	}
}
