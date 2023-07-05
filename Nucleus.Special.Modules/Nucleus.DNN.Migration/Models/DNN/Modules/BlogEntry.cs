using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules;

public class BlogEntry
{
  [Column("EntryID")]
  public int EntryId { get; set; }

  public Blog Blog { get; set; }

  public string Title { get; set; }

  public string Entry { get; set; }

  public string Description { get; set; }

  public Boolean Published { get; set; }
  public DateTime AddedDate { get; set; }

}
