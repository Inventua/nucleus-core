using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class LinksModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "Links";

  public override Guid? GetMatch(IEnumerable<ModuleDefinition> modules, DesktopModule desktopModule)
  {
    string[] matches = { "links", "dnn_links" };
    
    if (matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase))
    {
      return new("b516d8dd-c793-4776-be33-902eb704bef6");
    }

    return null;
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule)
  {
    throw new NotImplementedException();
  }
}
