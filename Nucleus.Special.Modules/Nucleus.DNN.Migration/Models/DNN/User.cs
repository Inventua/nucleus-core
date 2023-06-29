using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class User : DNNEntity
{
  public override int Id()
  {
    return this.UserId;    
  }
  public override string DisplayName()
  {
    return this.UserName;
  }

  [Column("UserID")]
  public int UserId { get; set; }

  public Boolean IsSuperUser { get; set; }
  

  public string UserName { get; set; }

  public string Email { get; set; }

  public string FirstName { get; set; }

  public string LastName { get; set; }


  public List<Models.DNN.Role> Roles { get; set; }

  public List<Models.DNN.UserProfileProperty> ProfileProperties { get; set; }

  public UserPortal UserPortal { get; set; }

}
