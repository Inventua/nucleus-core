using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using static Nucleus.Modules.Media.Controllers.MediaController;

namespace Nucleus.Modules.Media.Models;

public class Settings
{
  private class ModuleSettingsKeys
  {
    public const string MEDIA_SOURCE_TYPE = "media:sourcetype";

    public const string MEDIA_FILE_ID = "media:file:id";
    public const string MEDIA_URL = "media:url";
    public const string MEDIA_YOUTUBE_ID = "media:youtube-id";

    public const string MEDIA_CAPTION = "media:caption";
    public const string MEDIA_ALTERNATETEXT = "media:alternatetext";
    public const string MEDIA_SHOWCAPTION = "media:showcaption";

    public const string MEDIA_HEIGHT = "media:height";
    public const string MEDIA_WIDTH = "media:width";
    public const string MEDIA_ALWAYSDOWNLOAD = "media:alwaysdownload";
    public const string MEDIA_AUTOPLAY= "media:autoplay";
  }

  public static class AvailableSourceTypes
  {
    public const string File = Nucleus.Abstractions.Models.FileSystem.File.URN;
    public const string Url = "urn:url";
    public const string YouTube = "https://www.youtube.com/";
  }

  public string SourceType { get; set; }
  public File SelectedFile { get; set; }
  public Guid SelectedFileId { get; set; }

  public string YoutubeId { get; set; }
  public string Url { get; set; }


  public string Height { get; set; }
  public string Width { get; set; }

  public Boolean AlwaysDownload { get; set; }
  public Boolean AutoPlay { get; set; }

  public string Caption { get; set; }
  public string AlternateText { get; set; }
  public Boolean ShowCaption { get; set; }

  public void GetSettings(PageModule module)
  {
    if (module != null)
    {
      this.SourceType = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_SOURCE_TYPE, AvailableSourceTypes.File);

      this.SelectedFileId = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_FILE_ID, Guid.Empty);
      this.Url = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_URL, "");
      this.YoutubeId = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_YOUTUBE_ID, "");

      this.Caption = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_CAPTION, "");
      this.AlternateText = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_ALTERNATETEXT, "");
      this.ShowCaption = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_SHOWCAPTION, false);

      this.Height = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_HEIGHT, "");
      this.Width = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_WIDTH, "");
      this.AlwaysDownload = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_ALWAYSDOWNLOAD, false);
      this.AutoPlay = module.ModuleSettings.Get(ModuleSettingsKeys.MEDIA_AUTOPLAY, false);
    }
  }

  public void SetSettings(PageModule module)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_SOURCE_TYPE, this.SourceType);

    switch (this.SourceType)
    {
      case Models.Settings.AvailableSourceTypes.File:
        if (this.SelectedFile?.Path != null)
        {
          module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_FILE_ID, this.SelectedFile.Id);
        }
        else
        {
          module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_FILE_ID, Guid.Empty);
        }
        break;

      case Models.Settings.AvailableSourceTypes.Url:
        module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_URL, this.Url);
        break;

      case Models.Settings.AvailableSourceTypes.YouTube:
        module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_YOUTUBE_ID, this.YoutubeId);
        break;
    }

    module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_CAPTION, this.Caption);
    module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_ALTERNATETEXT, this.AlternateText);
    module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_SHOWCAPTION, this.ShowCaption);

    module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_HEIGHT, this.Height);
    module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_WIDTH, this.Width);
    module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_ALWAYSDOWNLOAD, this.AlwaysDownload);
    module.ModuleSettings.Set(ModuleSettingsKeys.MEDIA_AUTOPLAY, this.AutoPlay);
  }

}
