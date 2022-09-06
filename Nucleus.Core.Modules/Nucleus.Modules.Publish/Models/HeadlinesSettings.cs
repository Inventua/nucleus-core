using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Publish.Models
{
  public class HeadlinesSettings
  {

		private const string MODULESETTING_LINKED_MODULE_ID = "publish-headlines:linked-module-id";
		private const string MODULESETTING_LAYOUT = "publish-headlines:layout";
		private const string MODULESETTING_PUBLISHED_DATE_RANGE = "publish-headlines:filter:published-date-range";
		private const string MODULESETTING_FEATUREDONLY = "publish-headlines:filter:featured-only";
		private const string MODULESETTING_NUMBEROFARTICLES = "publish-headlines:filter:number-of-articles";

		//public const string MODULESETTING_CATEGORYLIST_ID = "publish-headlines:categorylist:id";

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

		public enum SortingFields
    {
			Featured,
			Date
    }

    public Guid LinkedModuleId { get; set; }

		public List<ModuleInstance> ModuleInstances { get; set; } = new();
		public string Layout { get; set; }

		#region "  Filters  "
		public PublishedDateRanges PublishedDateRange { get; set; } = PublishedDateRanges.Any;
		public Boolean FeaturedOnly { get; set; } = false;
		public int NumberOfArticles { get; set; } = 10;
		#endregion
		public List<int> DisplaySizes { get; set; } = new List<int>() { 10, 20, 30, 40, 50 };

		public SortingFields SortBy { get; set; } = SortingFields.Featured;

		public void GetSettings(PageModule module)
		{
			this.LinkedModuleId = module.ModuleSettings.Get(MODULESETTING_LINKED_MODULE_ID, Guid.Empty);
			this.Layout = module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");
			// filter settings
			this.PublishedDateRange = module.ModuleSettings.Get(MODULESETTING_PUBLISHED_DATE_RANGE, PublishedDateRanges.Any);
			this.FeaturedOnly = module.ModuleSettings.Get(MODULESETTING_FEATUREDONLY, false);
			this.NumberOfArticles = module.ModuleSettings.Get(MODULESETTING_NUMBEROFARTICLES, 10);
		}

		public void SetSettings(PageModule module)
    {
			module.ModuleSettings.Set(MODULESETTING_LINKED_MODULE_ID, this.LinkedModuleId);
			module.ModuleSettings.Set(MODULESETTING_LAYOUT, this.Layout);

			module.ModuleSettings.Set(MODULESETTING_PUBLISHED_DATE_RANGE, this.PublishedDateRange);
			module.ModuleSettings.Set(MODULESETTING_FEATUREDONLY, this.FeaturedOnly);
			module.ModuleSettings.Set(MODULESETTING_NUMBEROFARTICLES, this.NumberOfArticles);
		}

		public class ModuleInstance
		{
			public Guid ModuleId { get; set; }
			public string Caption { get; set; }
		}
	}
}
