using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class DocumentsModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "Documents";

  public override Guid? GetMatch(IEnumerable<ModuleDefinition> modules, DesktopModule desktopModule)
  {
    string[] matches = { "documents", "dnn_documents" };

    if (matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase))
    {
      return new("28df7ff3-6407-459e-8608-c1ef4181807c");
    }

    return null;
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule)
  {
    throw new NotImplementedException();
  }
}
