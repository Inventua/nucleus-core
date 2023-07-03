using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules;

public class Document
{
  [Column("ItemID")]
  public int ItemId { get; set; }

  [Column("ModuleID")]
  public int ModuleId { get; set; }

  [Column("URL")]
  public string Url { get; set; }

  public string Title { get; set; }

  public string Category { get; set; }

  public string Description { get; set; }

  public int SortOrderIndex { get; set; }
}
