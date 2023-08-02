using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class Version
{
  public int VersionId { get; set; }
  public int Major { get; set; }
  public int Minor { get; set; }
  public int Build { get; set; }

  public System.Version ToVersion()
  {
    return System.Version.Parse($"{this.Major}.{this.Minor}.{this.Build}.0");
  }

  public override string ToString()
  {
    return $"{this.Major}.{this.Minor}.{this.Build}";
  }
}
