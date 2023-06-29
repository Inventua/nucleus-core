using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class PageMigration : MigrationEngineBase<Models.DNN.Page>
{
  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }
  private IPageManager PageManager { get; }

  public PageMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IPageManager pageManager) : base("Pages")  
  { 
    this.Context = context;
    this.MigrationManager = migrationManager;
    this.PageManager = pageManager;
  }

  public override Task Migrate()
  {
    throw new NotImplementedException();
  }

  public override Task Validate()
  {
    return Task.CompletedTask;
  }
}
