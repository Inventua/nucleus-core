using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules;

public class ForumGroupSettings
{
  public int NTForums_GroupSettingsID { get; set; }

  public int ForumGroupID { get; set; }

  public Boolean AllowAttachments { get; set; }

  public Boolean UseFilter { get; set; }

  public Boolean Hidden { get; set; }

  public Boolean Active { get; set; }

  public Boolean AllowHTML { get; set; }

  public Boolean AllowScript { get; set; }

  public Boolean AllowSubscribe { get; set; }

  public Boolean AllowEmoticons { get; set; }

  public Boolean AllowPostIcon { get; set; }

  public Boolean IsModerated { get; set; }

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

  public Boolean IndexContent { get; set; }

}
