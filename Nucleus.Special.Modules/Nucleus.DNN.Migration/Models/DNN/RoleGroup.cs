using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN;

public class RoleGroup : DNNEntity
{
  public override int Id()
  {
    return this.RoleGroupId;
  }

  public override string DisplayName()
  {
    return this.RoleGroupName;
  }

  [Column("RoleGroupID")]
  public int RoleGroupId { get; set; }

  [Column("PortalID")]
  public int PortalId { get; set; }

  public string RoleGroupName { get; set; }
  public string Description { get; set; }

  public List<Role> Roles { get; set; }

  [NotMapped]
  public int RoleCount { get; set; }
}
