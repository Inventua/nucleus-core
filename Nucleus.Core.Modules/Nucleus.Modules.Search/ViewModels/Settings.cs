using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Search.ViewModels
{
	public class Settings
	{
		public enum DisplayModes
		{
			Full,
			Compact,
			Minimal
		}

    public enum SearchModes
    {
      Any,
      All
    }

    public const string PROMPT_DEFAULT = "Search Term";

		public DisplayModes DisplayMode { get; set;}

    public SearchModes SearchMode { get; set; }

    public string SearchCaption { get; set; }
		public string SearchButtonCaption { get; set; }

		public string Prompt { get; set; } = PROMPT_DEFAULT;


		public Boolean ShowUrl { get; set; }

		public Boolean ShowSummary { get; set; }

		public Boolean ShowCategories { get; set; }
		public Boolean ShowPublishDate { get; set; }
		public Boolean ShowSize { get; set; }
		public Boolean ShowScore { get; set; }
    public Boolean ShowScoreAssessment { get; set; }

    public Boolean ShowType { get; set; }

    public Guid ResultsPageId { get; set; }

		public Boolean IncludeFiles { get; set; } = true;

		public PageMenu PageMenu { get; set; }
		public string IncludeScopes { get; set; }
		public int MaximumSuggestions { get; set; }

		public List<AvailableSearchProvider> SearchProviders { get; set; }

		public string SearchProvider { get; set; }

		public class AvailableSearchProvider
		{
			public string Name { get; set; }
			public string ClassName { get; set; }

		}
	}
}
