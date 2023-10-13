using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static Nucleus.DNN.Migration.MigrationEngines.MigrationEngineBase;

namespace Nucleus.DNN.Migration.Models;

public class EngineProgress
{
  public string Title { get; init; }

  public int TotalCount { get; init; }

  public int Current { get; init; }

  public Boolean IsStarted { get; init; }

  public int CurrentPercent { get; init; }

  public EngineStates State { get; init; }

  public List<Models.DNN.DNNEntity> Items { get; init; }

  public DateTime? StartTime { get; init; }
}
