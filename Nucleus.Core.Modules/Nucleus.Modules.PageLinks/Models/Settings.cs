using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.PageLinks.Models;

public class Settings
{
	private class ModuleSettingsKeys
	{
		public const string MODULESETTING_TITLE = "pagelinks:title";
		public const string MODULESETTING_MODE = "pagelinks:mode";
		public const string MODULESETTING_INCLUDE_HEADERS = "pagelinks:include-headers";
		public const string MODULESETTING_HEADING_CLASS = "pagelinks:heading-class";
		public const string MODULESETTING_ROOT_SELECTOR = "pagelinks:root-selector";
	}

	public enum Modes
	{
		Automatic = 0,
		Manual = 1
	}

	[Flags]
	public enum HtmlHeaderTags
	{
		[Display(Name = "All")] All = 0,
		[Display(Name = "Heading 1 (H1)")] H1 = 1,
		[Display(Name = "Heading 2 (H2)")]	H2 = 2,
		[Display(Name = "Heading 3 (H3)")] H3 = 4,
		[Display(Name = "Heading 4 (H4)")] H4 = 8,
		[Display(Name = "Heading 5 (H5)")] H5 = 16,
		[Display(Name = "Heading 6 (H6)")] H6 = 32
	}

	public string Title { get; set; }
	public Modes Mode { get; set; }
	public HtmlHeaderTags IncludeHeaders { get; set; } //= HtmlHeaderTags.All;
	public string HeadingClass { get; set; }
	public string RootSelector { get; set; }
	public IList<String> PageLinks { get; set; }


	public void GetSettings(PageModule module)
	{
		this.Title = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_TITLE, "");
		this.Mode = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MODE, Modes.Automatic);
		this.HeadingClass = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_HEADING_CLASS, "");
		this.RootSelector = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_ROOT_SELECTOR, "");
		this.IncludeHeaders = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_INCLUDE_HEADERS, HtmlHeaderTags.All);
	}

	public void SetSettings(PageModule module)
	{
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_TITLE, this.Title);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MODE, this.Mode);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_HEADING_CLASS, this.HeadingClass);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_ROOT_SELECTOR, this.RootSelector);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_INCLUDE_HEADERS, this.IncludeHeaders);
	}
}
