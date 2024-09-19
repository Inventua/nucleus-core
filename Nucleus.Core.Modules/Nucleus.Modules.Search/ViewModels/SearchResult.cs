namespace Nucleus.Modules.Search.ViewModels;

public class SearchResult
{
  public Models.Settings Settings { get; set; }
  public Nucleus.Abstractions.Search.SearchResult Result { get; set; }

  public double? MaxScore { get; set; }

  public SearchResult(Models.Settings settings, double? maxScore, Abstractions.Search.SearchResult result)
  {
    this.Settings = settings;    
    this.MaxScore = maxScore;
    this.Result = result;
  }
}
