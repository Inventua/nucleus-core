using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.FileSystemProviders;

namespace Nucleus.ViewFeatures.ViewModels
{
	/// <summary>
	/// ViewModel for FolderSelector control.
	/// </summary>
	public class FolderSelector
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
		/// Selected folder property name in the caller's view model.
		/// </summary>
		/// <remarks>
		/// The specified property should be of type <see cref="Folder"/>
		/// </remarks>
		public string PropertyName { get; set; }

		/// <summary>
		/// List of file system providers.
		/// </summary>
		public IReadOnlyList<FileSystemProviderInfo> Providers { get; set; }

		/// <summary>
		/// Selected folder
		/// </summary>
		public Folder SelectedFolder { get; set; } = new();


		
	}
}
