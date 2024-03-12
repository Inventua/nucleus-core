using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Extensions
{
	/// <summary>
	/// Package validation result returned by the extension manager .ValidatePackage method.
	/// </summary>
	public class PackageResult
	{
		/// <summary>
		/// Indicates whether the package is valid
		/// </summary>
		public Boolean IsValid { get; set; }
		/// <summary>
		/// Name of automatically generated temporary file (copy)
		/// </summary>
		public string FileId { get; set; }

		/// <summary>
		/// Parsed package data
		/// </summary>
		public Package Package { get; set; }

		/// <summary>
		/// Package readme
		/// </summary>
		public string Readme { get; set; }

		/// <summary>
		/// Package license
		/// </summary>
		public string License { get; set; }

		/// <summary>
		/// Validation result
		/// </summary>
		public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary Messages { get; set; }

	}
}
