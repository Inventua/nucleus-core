using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.Web.ViewModels.Admin;

public class FileSystemSearchResults
{
  public string SearchTerm { get; set; }
  public PagingSettings PagingSettings { get; set; }
  public  Nucleus.Abstractions.Search.SearchResults Results { get; set; }
}
