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

public class MediaModuleContentMigration : ModuleContentMigrationBase
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

  public MediaModuleContentMigration(DNNMigrationManager dnnMigrationManager, ISiteManager siteManager, IFileSystemManager fileSystemManager)
  {
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override string ModuleFriendlyName => "Media";

  public override Guid ModuleDefinitionId => new("2ffdf8a4-edab-48e5-80c6-7b068e4721bb");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "media", "dnn_media" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage.SiteId);
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    Models.DNN.Modules.MediaSettings settings = await this.DnnMigrationManager.GetDnnMediaSettings(dnnModule.ModuleId);
    Abstractions.Models.FileSystem.File newMediaFile = null;

    if (settings.Source.StartsWith("FileID=", StringComparison.OrdinalIgnoreCase))
    {
      // file
      Models.DNN.File dnnMediaFile = await this.DnnMigrationManager.GetDnnFile(Int32.Parse(settings.Source.Replace("fileid=", "", StringComparison.OrdinalIgnoreCase)));

      try
      {
        newMediaFile = await this.FileSystemManager.GetFile(site, fileSystemProvider.Key, dnnMediaFile.Path());
      }
      catch (System.IO.FileNotFoundException)
      {
        dnnPage.AddWarning($"Media module '{dnnModule.ModuleTitle}' was not migrated because its file '{dnnMediaFile.Path()}' could not be found.");
        return;
      }
    }
    else
    {
      // unsupported type.  The Nucleus media module does not support Urls
      dnnPage.AddWarning($"Media module '{dnnModule.ModuleTitle}' was not migrated because it does not reference a file.");
      return;
    }

    newModule.ModuleSettings.Set(MEDIA_FILE_ID, newMediaFile.Id);

    newModule.ModuleSettings.Set(MEDIA_ALTERNATETEXT, settings.AlternateText);
    
    newModule.ModuleSettings.Set(MEDIA_HEIGHT, settings.Height);
    newModule.ModuleSettings.Set(MEDIA_WIDTH, settings.Width);

    // the DNN media module doesn't have settings for these
    //newModule.ModuleSettings.Set(MEDIA_ALWAYSDOWNLOAD = "media:alwaysdownload";
    //newModule.ModuleSettings.Set(MEDIA_CAPTION = "media:caption";
    //newModule.ModuleSettings.Set(MEDIA_SHOWCAPTION = "media:showcaption";

  }
}
