using System;
using Nucleus.Abstractions.Models;
using System.Collections.Generic;

namespace Nucleus.Web.ViewModels.Admin
{
  public class SearchSettings
  {
    public ApiKey ApiKey { get; set; }
    public IEnumerable<ApiKey> ApiKeys { get; set; }

		public Boolean ClearIndex { get; set; }
		public Boolean IndexPublicPagesOnly { get; set; }
    public Boolean IndexPublicFilesOnly { get; set; }
    
    public Boolean IndexPagesUseSsl { get; set; } = true;

    public List<AvailableSearchProvider> SearchProviders { get; set; }

    public string DefaultSearchProvider { get; set; }

    public class AvailableSearchProvider
    {
      public string Name { get; set; }
      public string ClassName { get; set; }

    }
  }
}
