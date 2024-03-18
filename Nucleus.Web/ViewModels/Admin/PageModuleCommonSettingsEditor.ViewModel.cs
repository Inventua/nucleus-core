using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Permissions;
using static Nucleus.Web.ViewModels.Admin.PageEditor;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class PageModuleCommonSettingsEditor 
	{
		public string UseLayout { get; set; }

    public PageEditorModes PageEditorMode { get; set; } = PageEditorModes.Default;


    public PageModule Module { get; set; } = new PageModule();


    public IEnumerable<SelectListItem> AvailableModules { get; set; }

    public PermissionsList ModulePermissions { get; set; } = new();


    public Guid SelectedModuleRoleId { get; set; }

    public IEnumerable<String> AvailablePanes { get; set; }
    public List<PermissionType> ModulePermissionTypes { get; set; }

    public IEnumerable<SelectListItem> AvailableModuleRoles { get; set; }

    public IEnumerable<ContainerDefinition> ModuleContainers { get; set; }

    public List<ContainerStyle> ModuleContainerStyles { get; set; }
    public IEnumerable<string> ModuleContainerStyleGroups { get; set; }
  }
}
