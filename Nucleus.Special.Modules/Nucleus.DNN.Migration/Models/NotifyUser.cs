using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models;

public class NotifyUser : DNNEntity
{
  //public Boolean IsSelected { get; set; } = true;
    
  //public Boolean CanSelect { get; set; } = true;

  public Nucleus.Abstractions.Models.User User { get; set; }

  public override string DisplayName()
  {
    return this.User.UserName;
  }

  public override int Id()
  {
    return int.MinValue;
  }
}
