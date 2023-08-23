using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class Settings
{
  public int ModuleId { get; set; }

  public string GroupKey { get; set; }

  public string SettingName { get; set; }
  public string SettingValue { get; set; }


}
