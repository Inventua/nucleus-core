using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.DNN;

namespace Nucleus.DNN.Migration.MigrationEngines;

public abstract class MigrationEngineBase<TModel> : MigrationEngineBase
  where TModel : Nucleus.DNN.Migration.Models.DNN.DNNEntity
{
  public MigrationEngineBase(string title) : base(title) { }


  private List<TModel> _items;

  public List<TModel> Items
  {
    get
    {
      return _items;
    }
    set
    {
      _items = value;
    }
  }

  public override List<DNNEntity> InnerItems
  {
    get
    {
      return this.Items.ToList<Models.DNN.DNNEntity>();
    }
  }

  virtual public void UpdateSelections(List<TModel> items)
  {
    if (items != null)
    {
      foreach (TModel item in items)
      {
        TModel existing = this.Items.Where(existing => existing.Id() == item.Id()).FirstOrDefault();
        if (existing != null)
        {
          existing.IsSelected = item.IsSelected && item.CanSelect;
        }
      }
    }
  }

  public void Init(List<TModel> items)
  {
    this.Items = items;
    this.TotalCount = 0;
  }

  public void SignalStart()
  {
    this.StartTime = DateTime.Now;

    this.IsStarted = true;
    this.TotalCount = this.Items.Where(item => item.CanSelect && item.IsSelected).Count();
    foreach (DNNEntity item in this.Items)
    {
      item.Results.Clear();
    }
  }

  public void SignalCompleted()
  {
    this.IsComplete = true;
    this.Current = this.TotalCount;
  }
}

public abstract class MigrationEngineBase
{
  public enum EngineStates
  {
    AwaitingInput,
    InProgress,
    Completed
  }

  public MigrationEngineBase(string title)
  {
    this.Title = $"{title}";
  }

  public string Title { get; }

  public string Message { get; set; }

  public DateTime? StartTime { get; set; }

  public abstract List<Nucleus.DNN.Migration.Models.DNN.DNNEntity> InnerItems { get; }

  public EngineProgress GetProgress()
  {
    EngineProgress copy = new Models.EngineProgress()
    {
      Title = this.Title,
      Current = this.Current,
      CurrentPercent = this.CurrentPercent,
      IsStarted = this.IsStarted,
      State = this.State(),
      TotalCount = this.TotalCount,
      StartTime = this.StartTime,
      Items = this.InnerItems.Where(item => item.IsSelected && item.CanSelect).ToList(),
    };

    return copy;
  }

  public int TotalCount { get; set; }

  public int Current { get; set; }

  public Boolean IsStarted { get; protected set; }

  public Boolean IsComplete { get; protected set; }


  public int CurrentPercent
  {
    get
    {
      if (this.TotalCount == 0 || this.Current == 0) return 0;
      return (int)((double)this.Current / (double)this.TotalCount * 100);
    }
  }

  public EngineStates State()
  {
    if (this.IsStarted && !this.IsComplete)
    {
      return EngineStates.InProgress;
    }
    else if (this.TotalCount > 0 && this.Current == 0)
    {
      return EngineStates.AwaitingInput;
    }
    return EngineStates.Completed;
  }


  public abstract Task Validate();

  public abstract Task Migrate(Boolean updateExisting);

  public void Start(int totalCount)
  {
    this.TotalCount = totalCount;
    this.Current = 0;
  }

  public void Progress()
  {
    if (this.TotalCount >= this.Current + 1)
    {
      this.Current++;
    }
  }

  public void Progress(int value)
  {
    if (this.TotalCount >= this.Current + value)
    {
      this.Current += value;
    }
  }
}