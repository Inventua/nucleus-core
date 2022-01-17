using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.ViewFeatures.ViewModels
{
	/// <summary>
	/// ViewModel for the FileUpload control.
	/// </summary>
	public class FileUpload
	{
		/// <summary>
		/// Name of the form field used to submit the uploaded file.
		/// </summary>
		public string ControlName { get; set; }

		/// <summary>
		/// Action name
		/// </summary>
		public string ActionName { get; set; }

		/// <summary>
		/// Controller Name
		/// </summary>
		public string ControllerName { get; set; }

		/// <summary>
		/// Area name
		/// </summary>
		public string AreaName { get; set; }

		/// <summary>
		/// Extension name
		/// </summary>
		public string ExtensionName { get; set; }

		/// <summary>
		/// Specifies whether uploads are enabled.
		/// </summary>
		public Boolean Enabled { get; set; }

		/// <summary>
		/// File filter.
		/// </summary>
		/// <remarks>
		/// This value is a comma-separated list of file types, and/or file type specifiers.
		/// </remarks>
		/// <example>
		/// @await Component.InvokeAsync(typeof(Nucleus.ViewFeatures.Controls.FileUpload), new { actionName = "UploadImageFile", Filter = "image/*" })
		/// </example>
		/// <example>
		/// @await Component.InvokeAsync(typeof(Nucleus.ViewFeatures.Controls.FileUpload), new { actionName = "UploadImageFile", Filter = ".gif,.png,.jpg,.jpeg,.bmp" })
		/// </example>
		public string Filter { get; set; }

	


	}
}
