using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Extensions;
using Nucleus.Modules.Publish.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish.DataProviders
{
	public class HeadlinesDataProvider : Nucleus.Data.EntityFramework.DataProvider, IHeadlinesDataProvider
	{
		protected new HeadlinesDbContext Context { get; }
		
		public HeadlinesDataProvider(HeadlinesDbContext context, ILogger<HeadlinesDataProvider> logger) : base(context, logger)
		{
			this.Context = context;
		}

		#region "    IHeadlinesDataProvider    "
		public async Task<FilterOptions> GetFilterOptions(PageModule pageModule)
		{
			FilterOptions result = new()
			{
				Categories =
					await this.Context.PublishHeadlinesFilterCategory
					.Where(article => EF.Property<Guid>(article, "ModuleId") == pageModule.Id)
					.Include(category => category.CategoryListItem)
					.AsSplitQuery()
					.AsNoTracking()
					.Select(category => category.CategoryListItem)
					.ToListAsync()
			};

			return result;
		}

    public async Task SaveFilterOptions(PageModule pageModule, FilterOptions options)
    {
			List<PublishHeadlinesFilterCategory> original = await this.Context.PublishHeadlinesFilterCategory
					.Where(article => EF.Property<Guid>(article, "ModuleId") == pageModule.Id)
					.Include(category => category.CategoryListItem)
					.AsSplitQuery()
					.ToListAsync();

			// delete removed categories
			if (original.Any())
			{
				foreach (PublishHeadlinesFilterCategory originalCategory in original)
				{
					Boolean found = false;

					foreach (ListItem newCategory in options.Categories)
					{
						if (newCategory.Id == originalCategory.CategoryListItem.Id)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						this.Context.PublishHeadlinesFilterCategory.Remove(originalCategory);
					}
				}
			}

			// create added categories
			foreach (ListItem category in options.Categories)
			{
				Boolean found = false;

				if (original.Any())
				{
					foreach (PublishHeadlinesFilterCategory originalCategory in original)
					{
						if (category.Id == originalCategory.CategoryListItem.Id)
						{
							found = true;
							break;
						}
					}
				}

				if (found)
				{
					// categories are never updated
				}
				else
				{
					PublishHeadlinesFilterCategory newCategory = new PublishHeadlinesFilterCategory() { CategoryListItem = category };
					this.Context.PublishHeadlinesFilterCategory.Add(newCategory);
					this.Context.Entry(newCategory).Property<Guid>("ModuleId").CurrentValue = pageModule.Id;
				}
			}

			await this.Context.SaveChangesAsync<PublishHeadlinesFilterCategory>();

		}
		#endregion
	}
}
