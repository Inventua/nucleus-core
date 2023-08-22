using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class PortalAlias
{
  [Column("PortalAliasID")]
  public int PortalAliasId { get; set; }


  [Column("PortalID")] 
  public int PortalId { get; set; }

  [Column("HTTPAlias")]
  public string HttpAlias { get; set; }

  public Boolean IsPrimary { get; set; }
}
