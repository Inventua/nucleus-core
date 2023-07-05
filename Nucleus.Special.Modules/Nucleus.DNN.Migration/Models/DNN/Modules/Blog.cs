using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules;

public class Blog
{
  [Column("PortalID")]
  public int PortalId { get; set; }

  [Column("BlogID")]
  public int BlogId { get; set; }

  public string Title { get; set; }

  public string Description { get; set; }

  public Boolean Public { get; set; }

  public List<BlogEntry> BlogEntries { get; set; }

}
