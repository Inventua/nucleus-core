using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public abstract class ModuleContentMigrationBase
{
  public abstract Guid ModuleDefinitionId { get; }

  public abstract Boolean IsMatch(Models.DNN.DesktopModule desktopModule);

  public abstract Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Nucleus.Abstractions.Models.Page newPage, Nucleus.Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys);

  public abstract string ModuleFriendlyName { get; }
}
