using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class PageModule
{  
  [Column("TabModuleID")]
  public int TabModuleId { get; set; }

  public Page Page { get; set; }

  public DesktopModule DesktopModule { get; set; }

  public int ModuleOrder { get; set; }
  public string PaneName { get; set; }

  public string ModuleTitle{ get; set; }

  public Boolean AllTabs { get; set; }
  public Boolean IsDeleted { get; set; }
  public Boolean DisplayTitle { get; set; }

  public Boolean InheritViewPermissions { get; set; }

  public List<PageModulePermission> Permissions { get; set; } 

}
