using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules;

public class MediaSettings
{
  [Column("ModuleID")]
  public int ModuleId { get; set; }

  [Column("src")]
  public string Source { get; set; }

  [Column("alt")]
  public string AlternateText { get; set; }

  [Column("height")]
  public int? Height { get; set; }

  [Column("width")]
  public int? Width { get; set; }


  // The ShowTitleLink and SortOrder settings are not migrated because the Nucleus documents module does not handle
  // options like these in the same way.  Instead,  Nucleus allows for user-defined Viewer Layouts, which is how you
  // would implement a default sort order / control whether the title is rendered as a link.
  // DisplayColumns is checked, and the Nucleus "show category", "show size", "show modified date" and "show description"
  // settings are populated based in its contents.

}
