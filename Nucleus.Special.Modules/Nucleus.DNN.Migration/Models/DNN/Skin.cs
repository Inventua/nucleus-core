using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class Skin
{
  public string SkinSrc { get; set; }
  public List<string> Panes { get; set; }
  public Guid AssignedLayoutId { get; set; }

  public string FriendlyName()
  {
    return this.SkinSrc.Replace("[G]Skins/", "", StringComparison.OrdinalIgnoreCase);
  }
}
