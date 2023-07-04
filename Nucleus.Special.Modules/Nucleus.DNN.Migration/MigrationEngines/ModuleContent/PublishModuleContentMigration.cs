using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class PublishModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "Publish";

  public override Guid ModuleDefinitionId => new("20af00b8-1d72-4c94-bce7-b175e0b173af");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "Blog" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    throw new NotImplementedException();
  }
}
