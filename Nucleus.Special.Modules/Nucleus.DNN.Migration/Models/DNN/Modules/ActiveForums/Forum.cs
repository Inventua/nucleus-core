using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class Forum : DNNEntity
{
  public override string DisplayName()
  {
    return Name;
  }

  public override int Id()
  {
    return ForumId; ;
  }

  [Column("ForumID")]
  public int ForumId { get; set; }

  public int PortalId { get; set; }

  public ForumGroup ForumGroup { get; set; }

  [Column("ForumName")]
  public string Name { get; set; }

  [Column("ForumDesc")]
  public string Description { get; set; }

  public int? SortOrder { get; set; }

  public Boolean Active { get; set; }
  public Boolean Hidden { get; set; }

  public string ForumSettingsKey { get; set; }

  public string ForumSecurityKey { get; set; }

  public ForumPermissions Permissions { get; set; }

  public List<ForumTopicLink> TopicLinks { get; set; }

  [NotMapped]
  public List<ForumSetting> Settings { get; set; }

  [NotMapped]
  public int PostCount { get; set; }

}

