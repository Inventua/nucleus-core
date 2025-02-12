﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.NTForums;

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

    public ForumGroup ForumGroup { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }

    public int? SortOrder { get; set; }


    public bool Active { get; set; }

    public bool Hidden { get; set; }

    public bool? InheritGroupSettings { get; set; }

    public bool IndexContent { get; set; }

    public bool IsModerated { get; set; }
    public int? AttachCount { get; set; }

    public string ModeratorRoles { get; set; }

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

    public int AttachMaxSize { get; set; }

    [NotMapped]
    public int PostCount { get; set; }

}

