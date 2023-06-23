using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class User
{
  [Column("UserID")]
  public int UserId { get; set; }

  [Column("PortalID")]
  public int PortalId { get; set; }

  public string UserName { get; set; }

  public string Email { get; set; }

  public List<Models.DNN.Role> Roles { get; set; }
}
