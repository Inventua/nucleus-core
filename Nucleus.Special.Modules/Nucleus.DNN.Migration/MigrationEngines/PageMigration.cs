using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class PageMigration : IMigrationEngine<Models.DNN.Page>
{
  public Task Migrate(List<Page> items)
  {
    throw new NotImplementedException();
  }

  public Task Validate(List<Page> items)
  {
    return Task.CompletedTask;
  }
}
