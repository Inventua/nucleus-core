using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.AcceptTerms.Models 
{
  public class UserAcceptedTerms : ModelBase
  {
    public Guid Id { get; set; }

    public Guid UserId { get; set; } 

    public DateTime DateAccepted { get; set; }
  }
}
