using System;
using Nucleus.Abstractions.Search;

namespace Nucleus.Modules.Search;

public static class SearchResultExtensions
{
  public static SearchResultAssessment GetAssessment(this SearchResult searchResult, double? maxScore, Boolean showScoreAssessment)
  {
    SearchResultAssessment assessment = new();

    if (searchResult.Score.HasValue && searchResult.Score.Value > 0)
    {
      if (maxScore.HasValue && maxScore.Value > 0)
      {
        // handle search providers which provide a "max" score, calculate a percentage
        assessment.Value = (searchResult.Score / maxScore.Value * 100);

        if (showScoreAssessment)
        {
          assessment.Text = assessment.Value switch
          {
            > 99 => "Best Match",
            > 75 => "Good Match",
            > 50 => "Medium Match",
            _ => "Low Match"
          };
        }

        if (!String.IsNullOrEmpty(assessment.Text))
        {
          assessment.Text += " - ";
        }

        assessment.Text += "Score:" + assessment.Value.Value.ToString("0.0") + "%";
      }
      else
      {
        // handle search providers which do not provide a "max" score, so we can't calculate a percentage
        assessment.Value = searchResult.Score.Value;
        assessment.Text = "Score:" + assessment.Value.Value.ToString("0.0");
      }
    }

    return assessment;
  }

  public class SearchResultAssessment
  {
    public double? Value { get; set; } = 0;
    public string Text { get; set; } = "";
  }
}

