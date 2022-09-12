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
  public class HeadlinesSettings : FilterOptionsBase
  {
		private const string MODULESETTING_LINKED_MODULE_ID = "publish-headlines:linked-module-id";
		private const string MODULESETTING_LAYOUT = "publish-headlines:layout";
		private const string MODULESETTING_SHOWPAGINGCONTROL = "publish-headlines:show-paging-control";
		private const string MODULESETTING_PUBLISHED_DATE_RANGE = "publish-headlines:filter:published-date-range";
		private const string MODULESETTING_FEATUREDONLY = "publish-headlines:filter:featured-only";
		private const string MODULESETTING_PAGESIZE = "publish-headlines:filter:pagesize";
		private const string MODULESETTING_SORTORDER = "publish-headlines:filter:sort-order";


		public Guid LinkedModuleId { get; set; }

		public List<ModuleInstance> ModuleInstances { get; set; } = new();

		public string Layout { get; set; }

		public Boolean ShowPagingControl { get; set; }

		public void GetSettings(PageModule module)
		{
			this.LinkedModuleId = module.ModuleSettings.Get(MODULESETTING_LINKED_MODULE_ID, Guid.Empty);
			this.Layout = module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");
			this.ShowPagingControl = module.ModuleSettings.Get(MODULESETTING_SHOWPAGINGCONTROL, true);
			// filter settings
			this.PublishedDateRange = module.ModuleSettings.Get(MODULESETTING_PUBLISHED_DATE_RANGE, PublishedDateRanges.Any);
			this.FeaturedOnly = module.ModuleSettings.Get(MODULESETTING_FEATUREDONLY, false);
			this.PageSize = module.ModuleSettings.Get(MODULESETTING_PAGESIZE, 0);
			this.SortOrder = module.ModuleSettings.Get(MODULESETTING_SORTORDER, SortOrders.FeaturedAndDate);
		}

		public void SetSettings(PageModule module)
    {
			module.ModuleSettings.Set(MODULESETTING_LINKED_MODULE_ID, this.LinkedModuleId);
			module.ModuleSettings.Set(MODULESETTING_LAYOUT, this.Layout);
			module.ModuleSettings.Set(MODULESETTING_SHOWPAGINGCONTROL, this.ShowPagingControl);

			module.ModuleSettings.Set(MODULESETTING_PUBLISHED_DATE_RANGE, this.PublishedDateRange);
			module.ModuleSettings.Set(MODULESETTING_FEATUREDONLY, this.FeaturedOnly);
			module.ModuleSettings.Set(MODULESETTING_PAGESIZE, this.PageSize);
			module.ModuleSettings.Set(MODULESETTING_SORTORDER, this.SortOrder);
		}

		public class ModuleInstance
		{
			public Guid ModuleId { get; set; }
			public string Caption { get; set; }
		}
	}
}
