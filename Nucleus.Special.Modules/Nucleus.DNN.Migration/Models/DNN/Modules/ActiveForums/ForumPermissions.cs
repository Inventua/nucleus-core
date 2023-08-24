using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class ForumPermissions
{
  public int PermissionsId { get; set; }

  [Column("CanView")]
  public string ViewRoles { get; set; }

  [Column("CanRead")]
  public string ReadPostRoles { get; set; }

  [Column("CanCreate")]
  public string CreatePostRoles { get; set; }

  [Column("CanReply")]
  public string ReplyPostRoles { get; set; }

  [Column("CanEdit")]
  public string EditPostRoles { get; set; }

  [Column("CanDelete")]
  public string DeletePostRoles { get; set; }

  [Column("CanLock")]
  public string LockPostRoles { get; set; }

  [Column("CanPin")]
  public string PinPostRoles { get; set; }

  [Column("CanAttach")]
  public string AttachToPostRoles { get; set; }

  [Column("CanSubscribe")]
  public string SubscribePostRoles { get; set; }

  [Column("CanModApprove")]
  public string ModeratorRoles { get; set; }
}
