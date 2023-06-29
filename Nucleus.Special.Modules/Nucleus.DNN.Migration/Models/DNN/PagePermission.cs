using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class PagePermission
{  
  [Column("TabPermissionID")]
  public int TabPermissionId { get; set; }

  public Page Page { get; set; }

  public Boolean AllowAccess{ get; set; }

  public Role Role { get; set; }

  public string PermissionCode { get; set; }

  public string PermissionKey { get; set; }
  public string PermissionName { get; set; }

}
