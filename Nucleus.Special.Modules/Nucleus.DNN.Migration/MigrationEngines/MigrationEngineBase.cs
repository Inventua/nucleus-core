using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.DNN.Migration.Models.DNN;

namespace Nucleus.DNN.Migration.MigrationEngines;

public abstract class MigrationEngineBase<TModel> : MigrationEngineBase 
  where TModel : Nucleus.DNN.Migration.Models.DNN.DNNEntity
{
  public MigrationEngineBase(string title) : base(title) { }

  private List<TModel> _items;

  public new List<TModel> Items 
  {
    get
    {
      return _items;
    }
    set
    {
      _items = value;
      base.Items = value.ToList<Models.DNN.DNNEntity>();
    }
  }

  public void Start(List<TModel> items)
  {
    this.Items = items;
    this.TotalCount = items.Count;
  }

  public abstract Task Validate(List<TModel> items);
 
  public abstract Task Migrate(List<TModel> items);

  public void AddWarning(int id, string message)
  {
    DNNEntity item = this.Items.Where(item=>item.Id() == id).FirstOrDefault();
    if (item != null)
    {
      item.Results.Add(new(ValidationResult.ValidationResultTypes.Warning, message));
    }
  }

  public void AddError(int id, string message)
  {
    DNNEntity item = this.Items.Where(item => item.Id() == id).FirstOrDefault();
    if (item != null)
    {
      item.Results.Add(new(ValidationResult.ValidationResultTypes.Error, message));      
    }    
  }
}

public class MigrationEngineBase
{
  public MigrationEngineBase(string title)
  {
    this.Title = title;
  }

  public string Title { get; }
  public List<Nucleus.DNN.Migration.Models.DNN.DNNEntity> Items { get; protected set; }

  public int TotalCount { get; set; }

  public int Current { get; set; }

  public int CurrentPercent
  {
    get
    {
      if (this.TotalCount == 0 || this.Current == 0) return 0;
      return (int)((double)this.Current / (double)this.TotalCount * 100);
    }
  }

  public Boolean Completed()
  {
    return this.TotalCount != 0 && this.Current >= this.TotalCount;
  }

  public void Start(int totalCount)
  {
    this.TotalCount=totalCount;
    this.Current = 0;
  }

  public void Progress()
  {
    this.Current++;
  }
}