using Nucleus.Abstractions.Models;
using System.Collections.Generic;

namespace Nucleus.Web.ViewModels.Admin
{
  public class SearchSettings
  {
    public ApiKey ApiKey { get; set; }
    public IEnumerable<ApiKey> ApiKeys { get; set; }

  }
}
