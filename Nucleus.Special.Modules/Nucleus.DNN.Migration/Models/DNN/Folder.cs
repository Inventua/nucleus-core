using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class Folder : DNNEntity
{
  public override int Id()
  {
    return this.FolderId;
  }

  public override string DisplayName()
  {
    return this.FolderPath;
  }

  [Column("FolderID")]
  public int FolderId { get; set; }

  [Column("PortalID")]
  public int? PortalId { get; set; }

  public string FolderPath { get; set; }


}
