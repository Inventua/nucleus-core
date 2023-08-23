using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class PageUrl 
{
  [Column("TabId")]
  public int PageId { get; set; }

  public int SeqNum { get; set; }

  public string Url { get; set; }

  public string QueryString { get; set; }

  public string HttpStatus { get; set; }


}
