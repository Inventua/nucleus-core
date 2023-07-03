using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class RegisterModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "";

  public override Guid? GetMatch(IEnumerable<ModuleDefinition> modules, DesktopModule desktopModule)
  {
    if (desktopModule.ModuleName.Equals("Security", StringComparison.OrdinalIgnoreCase))
    {
      return new("7b25bdaf-14a3-4bad-9c41-972dbbb384a1");
    }

    return null;
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule)
  {
    // The DNN user registration module doesn't have any settings or data
    return Task.CompletedTask;
  }
}
