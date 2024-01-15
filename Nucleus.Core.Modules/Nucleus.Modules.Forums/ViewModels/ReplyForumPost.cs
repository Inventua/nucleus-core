using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.ViewModels;

public class ReplyForumPost
{
  public const int MAX_LEVELS = 32;

  public Page Page { get; set; }

  public Forum Forum { get; set; }
  public Post Post { get; set; }

  public Reply Reply { get; set; }

  public Boolean CanAttach { get; set; }
  public Boolean CanSubscribe { get; set; }

  public Folder AttachmentsFolder { get; set; }
}
