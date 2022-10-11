using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Publish.Models
{
  public enum PublishedDateRanges
  {
    [Display(Name = "Any")] Any = 0,
    [Display(Name = "Last Week")] LastWeek = 100,
    [Display(Name = "Last Month")] LastMonth = 200,
    [Display(Name = "Last 3 Months")] Last3Months = 300,
    [Display(Name = "Last 6 Months")] Last6Months = 400,
    [Display(Name = "Last Year")] LastYear = 500,
    [Display(Name = "Last 2 Years")] Last2Years = 600
  }

  public enum SortOrders
  {
    [Display(Name = "Featured, Date")] FeaturedAndDate,
    [Display(Name = "Date")] Date
  }

  public class FilterOptions : ModelBase
  {
    public List<ListItem> Categories { get; set; } = new();

    public PublishedDateRanges PublishedDateRange { get; set; } = PublishedDateRanges.Any;
    public Boolean FeaturedOnly { get; set; } = false;

    public List<int> PageSizes { get; set; } = new List<int>() { 10, 20, 50, 100, 250 };

    public int PageSize { get; set; } = 10;

    public SortOrders SortOrder { get; set; } = SortOrders.FeaturedAndDate;
  }
}
