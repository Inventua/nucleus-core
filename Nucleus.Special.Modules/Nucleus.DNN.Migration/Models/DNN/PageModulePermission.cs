using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class PageModulePermission
{  
  [Column("ModulePermissionID")]
  public int ModulePermissionId { get; set; }
    
  public PageModule PageModule { get; set; }

  public Boolean AllowAccess{ get; set; }

  public int? RoleId { get; set; }
  public string RoleName { get; set; }

  [Column("UserID")]
  public int? UserId { get; set; }

  public string UserName { get; set; }

  public string PermissionCode { get; set; }

  public string PermissionKey { get; set; }
  public string PermissionName { get; set; }

}
