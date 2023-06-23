using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class Page
{
  [Column("PageID")]
  public int PageId { get; set; }

  [Column("PortalID")]
  public int PortalId { get; set; }

  public string PageName { get; set; }
  
  public string Title { get; set; }

}
