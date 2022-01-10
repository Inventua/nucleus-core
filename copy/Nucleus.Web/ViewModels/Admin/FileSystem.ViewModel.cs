using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Permissions;

namespace Nucleus.Web.ViewModels.Admin
{
	public class FileSystem
	{
		public string SelectedProviderKey { get; set; }
		public string NewFolder { get; set; }

		public ReadOnlyDictionary<string, Nucleus.Core.FileSystemProviders.FileSystemProviderInfo> Providers { get; set; }
		public Folder Folder { get; set; } = new();
		public FileSystemItem SelectedItem { get; set; }
		public Guid SelectedFolderRoleId { get; set; }

		public IEnumerable<Role> AvailableFolderRoles { get; set; }

		public List<PermissionType> FolderPermissionTypes { get; set; }

		public PermissionsList FolderPermissions { get; set; } = new();

	}
}
