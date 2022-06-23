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
		public PageEditorModes PageEditorMode { get; set; }

		public Boolean CanDeletePage { get; set; }
		public Page Page { get; set; } = new Page();

		public IEnumerable<Page> Pages { get; set; }
		public PageMenu PageMenu { get; set; }

		public PageModule Module { get; set; } = new PageModule();

		//public IEnumerable<ModuleDefinition> AvailableModules { get; set; } = new List<ModuleDefinition>();

		public IEnumerable<SelectListItem> AvailableModules { get; set; } 

		public IEnumerable<LayoutDefinition> Layouts { get; set; } 
		public IEnumerable<ContainerDefinition> Containers { get; set; }

		public Guid SelectedPageRoleId { get; set; }
		public Guid SelectedModuleRoleId { get; set; }

		public IEnumerable<SelectListItem> AvailablePageRoles { get; set; }
		//public IEnumerable<Role> AvailablePageRoles { get; set; } = new List<Role>();
		public IEnumerable<SelectListItem> AvailableModuleRoles { get; set; } 

		public IEnumerable<String> AvailablePanes { get; set; }

		public List<PermissionType> PagePermissionTypes { get; set; }
		public List<PermissionType> ModulePermissionTypes { get; set; }

		public PermissionsList PagePermissions { get; set; } = new();
		public PermissionsList ModulePermissions { get; set; } = new();

	}
}
