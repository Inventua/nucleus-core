using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class LoginModuleContentMigration : ModuleContentMigrationBase
{
  public override string ModuleFriendlyName => "Login";

  public override Guid? GetMatch(IEnumerable<ModuleDefinition> modules, DesktopModule desktopModule)
  {
    if (desktopModule.ModuleName.Equals("authentication", StringComparison.OrdinalIgnoreCase))
    {
      return new("f0a9ec71-c29e-436e-96e1-72dcdc44c32b");
    }

    return null;
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule)
  {
    throw new NotImplementedException();
  }
}
