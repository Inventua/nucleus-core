using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class UserProfileModuleContentMigration : ModuleContentMigrationBase
{
  public override Guid ModuleDefinitionId => new("1f347233-99e1-47b8-aa78-90ec16c6dbd2"); 
  
  public override string ModuleFriendlyName => "User Profile";

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    return desktopModule.ModuleName.Equals("ViewProfile", StringComparison.OrdinalIgnoreCase);
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    // The DNN user profile module doesn't have any settings or data
    return Task.CompletedTask;
  }
}
