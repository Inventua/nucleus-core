using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.FileSystemProviders;

namespace Nucleus.ViewFeatures.ViewModels
{
	/// <summary>
	/// View model for the FileSelector control.
	/// </summary>
	public class FileSelector
	{
		/// <summary>
		/// Area name
		/// </summary>
		public string AreaName { get; set; }
		
		/// <summary>
		/// Controller name
		/// </summary>
		public string ControllerName { get; set; }
		
		/// <summary>
		/// Extension name
		/// </summary>
		public string ExtensionName { get; set; }

		/// <summary>
		/// Set to false to suppress the "Select Another" button.
		/// </summary>
		public Boolean ShowSelectAnother { get; set; }

		/// <summary>
		/// Specifies the value added to the files list when there are no matching files to select.
		/// </summary>
		public string NoFilesMessage { get; set; } = "(no files)";

		/// <summary>
		/// Specifies whether to display a preview for image files.
		/// </summary>
		public Boolean ShowImagePreview { get; set; }
		
			/// <summary>
		/// Name of the action to execute when the user clicks "select another file"
		/// </summary>
		public string SelectAnotherActionName { get; set; }

		/// <summary>
		/// Property name for the selected file in the caller's view model. 
		/// </summary>
		/// <remarks>
		/// The specified property should be of type <see cref="File"/>
		/// </remarks>
		public string PropertyName { get; set; }

		/// <summary>
		/// List of available file providers
		/// </summary>
		public IReadOnlyList<FileSystemProviderInfo> Providers { get; set; }

		/// <summary>
		/// Selected Folder
		/// </summary>
		public Folder SelectedFolder { get; set; } = new();

		/// <summary>
		/// Selected File
		/// </summary>
		public File SelectedFile { get; set; } = new();

	}
}
