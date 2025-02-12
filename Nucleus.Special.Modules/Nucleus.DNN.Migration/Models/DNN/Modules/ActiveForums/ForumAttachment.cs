﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class ForumAttachment
{
    [Column("AttachID")]
    public int AttachmentId { get; set; }

    public ForumContent ForumContent { get; set; }

    [Column("UserID")]
    public int? UserId { get; set; }

    public string Filename { get; set; }

    public DateTime? DateAdded { get; set; }

    public DateTime? DateUpdated { get; set; }

    public string ContentType { get; set; }

    public int? FileSize { get; set; }
}
