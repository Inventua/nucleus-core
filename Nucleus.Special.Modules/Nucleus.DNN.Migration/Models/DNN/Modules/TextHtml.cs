using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules;

public class TextHtml
{
  [Column("ItemID")]
  public int ItemId { get; set; }

  [Column("ModuleID")]
  public int ModuleId { get; set; }

  public string Content { get; set; }

  public int Version { get; set; }

  public Boolean IsPublished { get;set; }
  

}
