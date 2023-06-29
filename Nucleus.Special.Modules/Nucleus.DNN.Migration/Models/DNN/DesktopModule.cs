using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class DesktopModule
{
  [Column("DesktopModuleID")] 
  public int DesktopModuleId { get; set; }

  public string FriendlyName { get; set; }

  public string Description { get; set; }
  public string Version { get; set; }

  public Boolean IsAdmin { get; set; }

  public string BusinessControllerClass { get; set; }

  public string ModuleName { get; set; }
}
