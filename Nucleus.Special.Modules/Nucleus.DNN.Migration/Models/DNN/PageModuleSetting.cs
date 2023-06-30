using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class PageModuleSetting
{  
  [Column("ModuleID")]
  public int ModuleId { get; set; }

  public string SettingName{ get; set; }

  public string SettingValue { get; set; }

}
