using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class File : DNNEntity
{
  public override int Id()
  {
    return this.FileId;
  }

  public override string DisplayName()
  {
    return this.FileName;
  }

  [Column("FileID")]
  public int FileId { get; set; }

  [Column("PortalID")]
  public int? PortalId { get; set; }

  public string FileName { get; set; }

  public Folder Folder { get; set; }

  public string Path()
  {
    return this.Folder?.FolderPath + this.FileName;
  }
}
