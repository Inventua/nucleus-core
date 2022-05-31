using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
//using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;

namespace Nucleus.Modules.StaticContent.Models
{
  public class Settings
  {
    public const string MODULESETTING_DEFAULT_FILE_ID = "staticcontent:defaultfile:id";

    public Guid DefaultFileId { get; set; }

    public void ReadSettings(PageModule module)
    {
      this.DefaultFileId = module.ModuleSettings.Get(MODULESETTING_DEFAULT_FILE_ID, Guid.Empty);
    }
  }
}
