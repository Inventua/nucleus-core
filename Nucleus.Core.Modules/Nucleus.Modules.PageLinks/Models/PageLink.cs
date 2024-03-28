using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.PageLinks.Models;

public class PageLink : ModelBase
{
  public Guid Id { get; set; }
  public string TargetId { get; set; }
  public string Title { get; set; }
  public int SortOrder { get; set; }
}
