using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class ForumContent
{
  [Column("ContentID")]
  public int ContentId { get; set; }

  public string Subject { get; set; }

  public string Summary { get; set; }

  public string Body { get; set; }

  public User User { get; set; }

  [Column("DateCreated")]
  public DateTime? DateAdded { get; set; }

  [Column("DateUpdated")]
  public DateTime? DateUpdated { get; set; }

  //public string UpdatedByUserID { get; set; }

  //public string Views { get; set; }
  //public string Replies { get; set; }

  [Column("IsDeleted")]
  public bool? Deleted { get; set; }


  //public string UserName { get; set; }

  //public string EmailAddress { get; set; }

  //public string IPAddress { get; set; }

  //public string StartDate { get; set; }
  //  public string EndDate { get; set; }


  public List<ForumAttachment> Attachments { get; set; }
}
