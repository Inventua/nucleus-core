using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class Container
{
  public string ContainerSrc { get; set; }
  public Guid AssignedContainerId { get; set; }

  public string FriendlyName()
  {
    return this.ContainerSrc.Replace("[G]Containers/", "", StringComparison.OrdinalIgnoreCase);
  }
}
