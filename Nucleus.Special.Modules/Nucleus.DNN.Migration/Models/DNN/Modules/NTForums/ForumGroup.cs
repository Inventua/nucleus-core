﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.NTForums;

public class ForumGroup
{
    [Column("ForumGroupID")]
    public int GroupId { get; set; }

    public ForumGroupSettings Settings { get; set; }

    [Column("ModuleID")]
    public int ModuleId { get; set; }

    [Column("PortalID")]
    public int PortalId { get; set; }


    [Column("GroupName")]
    public string Name { get; set; }

    public int? SortOrder { get; set; }

    public List<Forum> Forums { get; set; }
}
