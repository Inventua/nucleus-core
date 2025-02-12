﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.ViewModels;

public class ActiveForums
{
  public int PortalId { get; set; }

  public int TotalPosts { get; set; }

  public List<ForumGroup> ForumGroups { get; set; }

  // this is populated client-side
  public List<Forum> Forums { get; set; }

  public Boolean UpdateExisting { get; set; } = true;
  public Boolean ForumsNotInstalled { get; set; }

}
