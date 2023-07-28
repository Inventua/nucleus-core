using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class UserProfilePropertyDefinition
{
  [Column("PropertyDefinitionID")]
  public int PropertyDefinitionId { get; set; }

  [Column("PortalID")]
  public int? PortalId { get; set; }

  public Boolean Deleted { get; set; }

  public string PropertyName { get; set; }
  
}
