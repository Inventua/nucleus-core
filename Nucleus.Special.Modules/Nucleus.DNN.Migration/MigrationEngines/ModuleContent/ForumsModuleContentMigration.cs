using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class ForumsModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "Forums";

  public override Guid ModuleDefinitionId => new("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "NTForums", "dnn_documents" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    throw new NotImplementedException();
  }
}
