using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class File
{
  [Column("FileID")]
  public int FileId { get; set; }
  public string FileName { get; set; }
}
