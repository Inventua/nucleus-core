using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines;

public interface IMigrationEngine<TModel> where TModel : class
{
  public Task Validate(List<TModel> items);
  public Task Migrate(List<TModel> items);
}
