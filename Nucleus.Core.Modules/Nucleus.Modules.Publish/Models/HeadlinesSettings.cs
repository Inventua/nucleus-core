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
		private const string MODULESETTING_SHOWPAGINGCONTROL = "publish-headlines:show-paging-control";
		private const string MODULESETTING_PAGINGCONTROLMODE = "publish-headlines:paging-control:rendermode";
		private const string MODULESETTING_PUBLISHED_DATE_RANGE = "publish-headlines:filter:published-date-range";
		private const string MODULESETTING_FEATUREDONLY = "publish-headlines:filter:featured-only";
		private const string MODULESETTING_PAGESIZE = "publish-headlines:filter:pagesize";
		private const string MODULESETTING_SORTORDER = "publish-headlines:filter:sort-order";
		

		public Guid LinkedModuleId { get; set; }

		public List<ModuleInstance> ModuleInstances { get; set; } = new();

		public string Layout { get; set; }

		public Boolean ShowPagingControl { get; set; }

    public FilterOptions FilterOptions { get; set; } = new();

    public LayoutOptions LayoutOptions { get; set; } = new();

		public Nucleus.ViewFeatures.ViewModels.PagingControl.RenderModes PagingControlRenderMode { get; set; } = ViewFeatures.ViewModels.PagingControl.RenderModes.Standard;

		public void GetSettings(PageModule module)
		{
			this.LinkedModuleId = module.ModuleSettings.Get(MODULESETTING_LINKED_MODULE_ID, Guid.Empty);
			this.Layout = module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");
			this.ShowPagingControl = module.ModuleSettings.Get(MODULESETTING_SHOWPAGINGCONTROL, true);
			this.PagingControlRenderMode = module.ModuleSettings.Get(MODULESETTING_PAGINGCONTROLMODE, Nucleus.ViewFeatures.ViewModels.PagingControl.RenderModes.Standard);

			// filter settings
			this.FilterOptions.PublishedDateRange = module.ModuleSettings.Get(MODULESETTING_PUBLISHED_DATE_RANGE, PublishedDateRanges.Any);
			this.FilterOptions.FeaturedOnly = module.ModuleSettings.Get(MODULESETTING_FEATUREDONLY, false);
			this.FilterOptions.PageSize = module.ModuleSettings.Get(MODULESETTING_PAGESIZE, 0);
			this.FilterOptions.SortOrder = module.ModuleSettings.Get(MODULESETTING_SORTORDER, SortOrders.FeaturedAndDate);

			// layout settings
			this.LayoutOptions.GetSettings(module);
    }

    public void SetSettings(PageModule module)
    {
			module.ModuleSettings.Set(MODULESETTING_LINKED_MODULE_ID, this.LinkedModuleId);
			module.ModuleSettings.Set(MODULESETTING_LAYOUT, this.Layout);
			module.ModuleSettings.Set(MODULESETTING_SHOWPAGINGCONTROL, this.ShowPagingControl);
			module.ModuleSettings.Set(MODULESETTING_PAGINGCONTROLMODE, this.PagingControlRenderMode);

			module.ModuleSettings.Set(MODULESETTING_PUBLISHED_DATE_RANGE, this.FilterOptions.PublishedDateRange);
			module.ModuleSettings.Set(MODULESETTING_FEATUREDONLY, this.FilterOptions.FeaturedOnly);
			module.ModuleSettings.Set(MODULESETTING_PAGESIZE, this.FilterOptions.PageSize);
			module.ModuleSettings.Set(MODULESETTING_SORTORDER, this.FilterOptions.SortOrder);

			this.LayoutOptions.SetSettings(module);
    }

		public class ModuleInstance
		{
			public Guid ModuleId { get; set; }
			public string Caption { get; set; }
		}
	}
}
