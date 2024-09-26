using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Nucleus.Abstractions.Search;

namespace Nucleus.Modules.Search;

public static class SearchResultExtensions
{
  public static string GetUrl(this SearchResult searchResult, Boolean includeTextFragments)
  {
    StringBuilder url = new(searchResult.Url.StartsWith('/') ? "~" + searchResult.Url : searchResult.Url);

    if (includeTextFragments && searchResult.MatchedTerms != null && searchResult.MatchedTerms.Any())
    {
      if (!searchResult.Url.Contains("#"))
      {
        url.Append('#');
      }
      url.Append($":~:text={String.Join("&text=", searchResult.MatchedTerms)}");
    }

    return url.ToString();
  }

  public static string GetTitleText(this SearchResult searchResult)
  {
    if (String.IsNullOrEmpty(searchResult.Title)) return "";
    // remove <em> and </em> tags from the title.
    return System.Text.RegularExpressions.Regex.Replace(searchResult.Title, "<[^>]*>", "");        
  }

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

