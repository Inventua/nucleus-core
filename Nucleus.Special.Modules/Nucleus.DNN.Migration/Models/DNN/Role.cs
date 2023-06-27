using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class Role : DNNEntity
{
  public override int Id()
  {
    return this.RoleId; 
  }
 
  [Column("RoleID")]
  public int RoleId { get; set; }

  [Column("PortalID")]
  public int PortalId { get; set; }

  public string RoleName { get; set; }
  public string Description { get; set; }

  public Boolean AutoAssignment { get; set; }

  public RoleGroup RoleGroup { get; set; }

  public List<Models.DNN.User> Users { get; set; }

  [NotMapped]
  public int UserCount { get; set; }

}
