using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Permissions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class PageEditor 
	{
		public enum PageEditorModes
		{
			Default,
			Single,
			Standalone
		}

		public string UseLayout { get; set; }

    public PageEditorModes PageEditorMode { get; set; } = PageEditorModes.Default;

		public Boolean CanDeletePage { get; set; }

		public Page Page { get; set; } = new Page();

		public PageMenu ParentPageMenu { get; set; }
    public PageMenu LinkPageMenu { get; set; }


    //public PageModule Module { get; set; } = new PageModule();

		//public IEnumerable<SelectListItem> AvailableModules { get; set; } 

		public IEnumerable<LayoutDefinition> Layouts { get; set; } 

		public IEnumerable<ContainerDefinition> PageContainers { get; set; }
    //public IEnumerable<ContainerDefinition> ModuleContainers { get; set; }

    public Guid SelectedPageRoleId { get; set; }

		//public Guid SelectedModuleRoleId { get; set; }

		public IEnumerable<SelectListItem> AvailablePageRoles { get; set; }

		//public IEnumerable<SelectListItem> AvailableModuleRoles { get; set; } 

		public IEnumerable<String> PagePanes { get; set; }

		public List<PermissionType> PagePermissionTypes { get; set; }

		//public List<PermissionType> ModulePermissionTypes { get; set; }

		public PermissionsList PagePermissions { get; set; } = new();

		//public PermissionsList ModulePermissions { get; set; } = new();

		public Boolean DisableCopy { get; set; }

    public Nucleus.Abstractions.Models.FileSystem.File SelectedLinkFile { get; set; }


    //public List<ContainerStyle> ModuleContainerStyles { get; set; }
    //public IEnumerable<string> ModuleContainerStyleGroups { get; set; }

  }
}
