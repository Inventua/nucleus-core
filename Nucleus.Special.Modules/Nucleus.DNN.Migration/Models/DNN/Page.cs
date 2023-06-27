using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class Page : DNNEntity
{
  public override int Id()
  {
    return this.PageId;
  }

  [Column("TabID")]
  public int PageId { get; set; }

  public int? ParentId { get; set; }

  [Column("PortalID")]
  public int? PortalId { get; set; }

  [Column("TabName")]
  public string PageName { get; set; }
  
  public string Title { get; set; }
  public string Description { get; set; }

  public string Keywords { get; set; }

  public Boolean IsDeleted { get; set; }
  public Boolean IsVisible { get; set; }
  public Boolean DisableLink { get; set; }
  public Boolean PermanentRedirect { get; set; }

  public string TabPath { get; set; }
  public int TabOrder { get; set; }
  public int Level { get; set; }

  public string SkinSrc { get; set; }
  public string ContainerSrc { get; set; }

  public string Url { get; set; }

}
