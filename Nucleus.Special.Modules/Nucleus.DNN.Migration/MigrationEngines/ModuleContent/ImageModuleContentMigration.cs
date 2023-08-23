using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.FileSystemProviders;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class ImageModuleContentMigration : ModuleContentMigrationBase
{
  private DNNMigrationManager DnnMigrationManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private ISiteManager SiteManager { get; }

  public const string MEDIA_FILE_ID = "media:file:id";

  public const string MEDIA_CAPTION = "media:caption";
  public const string MEDIA_ALTERNATETEXT = "media:alternatetext";
  public const string MEDIA_SHOWCAPTION = "media:showcaption";

  public const string MEDIA_HEIGHT = "media:height";
  public const string MEDIA_WIDTH = "media:width";
  public const string MEDIA_ALWAYSDOWNLOAD = "media:alwaysdownload";

  public ImageModuleContentMigration(DNNMigrationManager dnnMigrationManager, ISiteManager siteManager, IFileSystemManager fileSystemManager)
  {
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override string ModuleFriendlyName => "Media";

  public override Guid ModuleDefinitionId => new("2ffdf8a4-edab-48e5-80c6-7b068e4721bb");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "image", "dnn_image" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    const string DNN_IMAGE_ALT_TEXT_SETTING_KEY = "alt";
    const string DNN_IMAGE_HEIGHT_SETTING_KEY = "height";
    const string DNN_IMAGE_SRC_SETTING_KEY = "src";
    const string DNN_IMAGE_WIDTH_SETTING_KEY = "width";

    string dnnAltTextSetting = "";
    string dnnHeightSetting = "";
    string dnnSourceSetting = "";
    string dnnWidthSetting = "";

    Site site = await this.SiteManager.Get(newPage.SiteId);
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    foreach (Models.DNN.PageModuleSetting setting in dnnModule.Settings)
    {
      switch (setting.SettingName)
      {
        case DNN_IMAGE_ALT_TEXT_SETTING_KEY:
          dnnAltTextSetting = setting.SettingValue;
          break;
        case DNN_IMAGE_WIDTH_SETTING_KEY:
          dnnWidthSetting = setting.SettingValue;
          break;
        case DNN_IMAGE_HEIGHT_SETTING_KEY:
          dnnHeightSetting = setting.SettingValue;
          break;
        case DNN_IMAGE_SRC_SETTING_KEY:
          dnnSourceSetting = setting.SettingValue;
          break;
      }
    }

    Abstractions.Models.FileSystem.File newImageFile = null;

    if (dnnSourceSetting.StartsWith("FileID=", StringComparison.OrdinalIgnoreCase))
    {
      // file
      Models.DNN.File dnnImageFile = await this.DnnMigrationManager.GetDnnFile(Int32.Parse(dnnSourceSetting.Replace("fileid=", "", StringComparison.OrdinalIgnoreCase)));

      try
      {
        newImageFile = await this.FileSystemManager.GetFile(site, fileSystemProvider.Key, dnnImageFile.Path());
      }
      catch (System.IO.FileNotFoundException)
      {
        dnnPage.AddWarning($"Image module '{dnnModule.ModuleTitle}' was not migrated because its file '{dnnImageFile.Path()}' could not be found.");
        return;
      }
    }
    else
    {
      // unsupported type.  The Nucleus Image module does not support Urls
      dnnPage.AddWarning($"Image module '{dnnModule.ModuleTitle}' was not migrated because it does not reference a file.");
      return;
    }

    newModule.ModuleSettings.Set(MEDIA_FILE_ID, newImageFile.Id);

    newModule.ModuleSettings.Set(MEDIA_ALTERNATETEXT, dnnAltTextSetting);

    newModule.ModuleSettings.Set(MEDIA_HEIGHT, dnnHeightSetting);
    newModule.ModuleSettings.Set(MEDIA_WIDTH, dnnWidthSetting);

  }
}
