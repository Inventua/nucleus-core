using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules;

public class DocumentsSettings
{
  [Column("ModuleID")]
  public int ModuleId { get; set; }

  public Boolean? AllowUserSort { get; set; }

  public Boolean? UseCategoriesList { get; set; }

  public string CategoriesListName { get; set; }

  public string DisplayColumns { get; set; }
  public string DefaultFolder { get; set; }


  // The ShowTitleLink and SortOrder settings are not migrated because the Nucleus documents module does not handle
  // options like these in the same way.  Instead,  Nucleus allows for user-defined Viewer Layouts, which is how you
  // would implement a default sort order / control whether the title is rendered as a link.
  // DisplayColumns is checked, and the Nucleus "show category", "show size", "show modified date" and "show description"
  // settings are populated based in its contents.

}
