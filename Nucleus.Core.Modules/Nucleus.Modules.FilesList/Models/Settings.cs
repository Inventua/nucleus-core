using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using System;

namespace Nucleus.Modules.FilesList.Models
{
	public class Settings
	{
		private class ModuleSettingsKeys
		{
			public const string MODULESETTING_SOURCE_FOLDER_ID = "fileslist:source-folder:id";
			public const string MODULESETTING_LAYOUT = "fileslist:layout";
			public const string MODULESETTING_SHOW_MODIFIED_DATE = "fileslist:show-modified-date";
			public const string MODULESETTING_SHOW_SIZE = "fileslist:show-size";
			public const string MODULESETTING_SHOW_DIMENSIONS = "fileslist:show-dimensions";
		}

		public Guid SourceFolderId { get; set; }
		public string Layout {  get; set; }
		public Boolean ShowModifiedDate { get; set; }
		public Boolean ShowSize { get; set; }
		public Boolean ShowDimensions { get; set; }

		public void GetSettings(PageModule module)
		{
			this.SourceFolderId = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SOURCE_FOLDER_ID, Guid.Empty);
			this.Layout = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_LAYOUT, "Table");
			this.ShowModifiedDate = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_MODIFIED_DATE, true);
			this.ShowSize = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_SIZE, true);
			this.ShowDimensions = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_DIMENSIONS, false);
		}

		public void SetSettings(PageModule module)
		{
			module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SOURCE_FOLDER_ID, this.SourceFolderId);
			module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_LAYOUT, this.Layout);
			module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_MODIFIED_DATE, this.ShowModifiedDate);
			module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_SIZE, this.ShowSize);
			module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_DIMENSIONS, this.ShowDimensions);
		}
	}

}
