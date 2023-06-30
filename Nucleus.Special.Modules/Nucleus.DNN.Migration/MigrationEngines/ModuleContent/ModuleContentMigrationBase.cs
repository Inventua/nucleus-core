using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public abstract class ModuleContentMigrationBase
{
  public abstract Guid? GetMatch(IEnumerable<Nucleus.Abstractions.Models.ModuleDefinition> modules, Models.DNN.DesktopModule desktopModule);

  public abstract Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Nucleus.Abstractions.Models.Page newPage, Nucleus.Abstractions.Models.PageModule newModule);

  public abstract string ModuleFriendlyName { get; }
}
