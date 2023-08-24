using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class UserPortal
{
  public int UserPortalId { get; set; }

  public int UserId { get; set; }

  public int PortalId { get; set; } 

  public Portal Portal { get; set; }

  public Boolean Authorised { get; set; }

  public Boolean IsDeleted { get; set; }

  //public User User { get; set; }

}
