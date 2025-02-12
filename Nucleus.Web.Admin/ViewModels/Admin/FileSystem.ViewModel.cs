﻿using System;
using System.Collections.Generic;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Permissions;
using Nucleus.Abstractions.Models.Paging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Mvc;
using Nucleus.ViewFeatures;

namespace Nucleus.Web.ViewModels.Admin
{
	public class FileSystem
	{
		public string SelectedProviderKey { get; set; }
		public string NewFolder { get; set; }

		public IReadOnlyList<FileSystemProviderInfo> Providers { get; set; }
		public Folder Folder { get; set; } = new();

    public PagingSettings PagingSettings { get; set; }

    public List<Folder> Folders { get; set; } = new();
    public List<Nucleus.Abstractions.Models.FileSystem.File> Files { get; set; } = new();
    public Guid SelectedFileId { get; set; }

    public List<Folder> Ancestors { get; set; } = new();

		public FileSystemItem SelectedItem { get; set; }
		public Guid SelectedFolderRoleId { get; set; }

		public IEnumerable<SelectListItem> AvailableFolderRoles { get; set; }

		public List<PermissionType> FolderPermissionTypes { get; set; }

		public PermissionsList FolderPermissions { get; set; } = new();

		public Boolean EnableDelete { get; set; }
		public Boolean EnableRename { get; set; }
		public Boolean DisableCopy { get; set; }

    public string SearchTerm { get; set; }
  }
}
