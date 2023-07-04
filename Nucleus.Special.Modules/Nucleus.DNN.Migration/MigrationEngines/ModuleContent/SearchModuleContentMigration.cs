using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class SearchModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "Search";

  public override Guid ModuleDefinitionId => new("c225f388-e5c1-48e2-9352-237cd8d0d2b6");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "SearchResults", "SearchInput" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    throw new NotImplementedException();
  }
}
