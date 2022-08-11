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
		
		public DisplayModes DisplayMode { get; set;}
		public string SearchCaption { get; set; }
		public string SearchButtonCaption { get; set; }	

		public Boolean ShowUrl { get; set; }

		public Boolean ShowSummary { get; set; }

		public Boolean ShowCategories { get; set; }
		public Boolean ShowPublishDate { get; set; }
		public Boolean ShowSize { get; set; }
		public Boolean ShowScore { get; set; }

		public Guid ResultsPageId { get; set; }

		public Boolean IncludeFiles { get; set; } = true;

		public PageMenu PageMenu { get; set; }
		public string IncludeScopes { get; set; }
		public int MaximumSuggestions { get; set; }
	}
}
