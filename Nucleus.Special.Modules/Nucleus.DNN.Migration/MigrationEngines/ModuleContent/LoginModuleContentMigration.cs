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

  public override Guid ModuleDefinitionId => new("f0a9ec71-c29e-436e-96e1-72dcdc44c32b");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    return desktopModule.ModuleName.Equals("authentication", StringComparison.OrdinalIgnoreCase);
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    // The DNN login module doesn't have any settings or data
    return Task.CompletedTask;
  }
}
