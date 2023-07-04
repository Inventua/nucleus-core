using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class MediaModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "Media";

  public override Guid ModuleDefinitionId => new("2ffdf8a4-edab-48e5-80c6-7b068e4721bb");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "media", "dnn_media" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    // TODO:
    throw new NotImplementedException();
  }
}
