using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models;

public class MigrationHistory : ModelBase
{
  public Guid Id { get; set; }

  public string SourceEntity { get; set; }

  public string SourceKey { get; set; }

  public string TargetEntity { get; set; }

  public string TargetKey { get; set; }

}
