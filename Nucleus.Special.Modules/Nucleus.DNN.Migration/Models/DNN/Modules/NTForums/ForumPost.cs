using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.NTForums;

public class ForumPost
{
    [Column("PostID")]
    public int PostId { get; set; }

    [Column("ForumID")]
    public int ForumId { get; set; }


    [Column("ParentPostID")]
    public int ParentPostId { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public User User { get; set; }

    public bool? Pinned { get; set; }

    public bool? Locked { get; set; }

    public DateTime? DateAdded { get; set; }

    public DateTime? DateUpdated { get; set; }

    //public string UpdatedByUserID { get; set; }

    //public string Views { get; set; }
    //public string Replies { get; set; }

    public bool? Deleted { get; set; }

    //public string LastPostID { get; set; }
    //public string DateLastPost { get; set; }
    public bool? Approved { get; set; }

    //public string UserName { get; set; }

    //public string EmailAddress { get; set; }

    //public string IPAddress { get; set; }

    public bool? IsAnnounce { get; set; }

    //public string StartDate { get; set; }
    //  public string EndDate { get; set; }

    public int Status { get; set; }

    public string PostType { get; set; }

    public bool Archived { get; set; }

    public List<ForumPostAttachment> Attachments { get; set; }
}
