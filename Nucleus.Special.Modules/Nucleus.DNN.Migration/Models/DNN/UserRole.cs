using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class UserRole
{
  [Column("UserID")]
  public int UserId { get; set; }

  [Column("RoleID")]
  public int RoleId { get; set; }

  public DateTime? Expiry { get; set; }

  public DateTime? EffectiveDate { get; set; }

  public Boolean? IsTrialUsed { get; set; }

}
