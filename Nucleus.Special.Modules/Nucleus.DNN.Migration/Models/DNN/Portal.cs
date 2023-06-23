using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class Portal
{
  [Column("PortalID")] 
  public int PortalId { get; set; }
  public string PortalName { get; set; }

}
