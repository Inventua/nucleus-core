using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class UserProfileProperty
{
  [Column("ProfileID")] 
  public int ProfileId { get; set; }

  [Column("UserID")]
  public int UserId { get; set; }

  [Column("PropertyDefinitionID")]
  public int PropertyDefinitionId { get; set; }

  public UserProfilePropertyDefinition PropertyDefinition { get; set; }

  [Column("PropertyValue")]
  public string Value { get; set; }

  public User User { get; set; }

}
