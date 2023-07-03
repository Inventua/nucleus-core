using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class ListItem : DNNEntity
{
  public override int Id()
  {
    return this.EntryId;
  }

  public override string DisplayName()
  {
    return this.Text;
  }

  [Column("EntryID")]
  public int EntryId { get; set; }

  [Column("PortalID")]
  public int? PortalId { get; set; }

  public string ListName { get; set; }

  public string Value { get; set; }
  public string Text { get; set; }

  public int SortOrder { get; set; }

  public Boolean SystemList { get; set; }

}
