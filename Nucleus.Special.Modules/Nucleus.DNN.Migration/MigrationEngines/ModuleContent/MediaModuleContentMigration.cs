using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class MeduaModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "Media";

  public override Guid? GetMatch(IEnumerable<ModuleDefinition> modules, DesktopModule desktopModule)
  {
    string[] matches = { "media", "dnn_media" };

    if (matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase))
    {
      return new("2ffdf8a4-edab-48e5-80c6-7b068e4721bb");
    }

    return null;
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule)
  {
    throw new NotImplementedException();
  }
}
