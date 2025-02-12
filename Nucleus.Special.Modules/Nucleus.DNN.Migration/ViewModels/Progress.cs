﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.DNN.Migration.MigrationEngines;
using Nucleus.DNN.Migration.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.ViewModels;

public class Progress
{
  public string Title { get; set; }

  public string Message { get; set; }

  public IEnumerable<EngineProgress> EngineProgress { get; set; }
  public Boolean InProgress { get; set; }

  public int ProgressInverval { get; set; }
}
