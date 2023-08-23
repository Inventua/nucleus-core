using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.NTForums;

public class ForumGroupSettings
{
    public int NTForums_GroupSettingsID { get; set; }

    public int ForumGroupID { get; set; }

    public bool AllowAttachments { get; set; }

    public bool UseFilter { get; set; }

    public bool Hidden { get; set; }

    public bool Active { get; set; }

    public bool AllowHTML { get; set; }

    public bool AllowScript { get; set; }

    public bool AllowSubscribe { get; set; }

    public bool AllowEmoticons { get; set; }

    public bool AllowPostIcon { get; set; }

    public bool IsModerated { get; set; }

    [Column("CanView")]
    public string ViewRoles { get; set; }

    [Column("CanCreate")]
    public string CreatePostRoles { get; set; }

    [Column("CanReply")]
    public string ReplyPostRoles { get; set; }

    [Column("CanLock")]
    public string LockPostRoles { get; set; }

    [Column("CanPin")]
    public string PinPostRoles { get; set; }

    [Column("CanEdit")]
    public string EditPostRoles { get; set; }

    [Column("CanDelete")]
    public string DeletePostRoles { get; set; }

    [Column("CanRead")]
    public string ReadPostRoles { get; set; }

    [Column("CanAttach")]
    public string AttachToPostRoles { get; set; }

    [Column("CanSubscribe")]
    public string SubscribePostRoles { get; set; }

    public string ModeratorRoles { get; set; }


    public int? AttachCount { get; set; }

    public bool IndexContent { get; set; }

}
