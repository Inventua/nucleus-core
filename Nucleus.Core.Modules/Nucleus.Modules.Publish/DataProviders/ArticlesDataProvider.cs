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
	public class ArticlesDataProvider : Nucleus.Data.EntityFramework.DataProvider, IArticlesDataProvider
	{
		protected IEventDispatcher EventManager { get; }
		protected new ArticlesDbContext Context { get; }

		public ArticlesDataProvider(ArticlesDbContext context, IEventDispatcher eventManager, ILogger<ArticlesDataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}

		#region "    IArticlesDataProvider    "
		public async Task<Article> Get(Guid id)
		{
			return await this.Context.Articles
				.Where(article => article.Id == id)
				.Include(article => article.Categories)
					.ThenInclude(category => category.CategoryListItem)
				.Include(article => article.Attachments)
					.ThenInclude(attachment=>attachment.File)
				.Include(article => article.ImageFile)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<Guid?> Find(PageModule module, string encodedTitle)
		{
			return await this.Context.Articles
				.Where(article => EF.Property<Guid>(article, "ModuleId") == module.Id && EF.Property<string>(article, "EncodedTitle") == encodedTitle)
				.Select(article => article.Id)
				.FirstOrDefaultAsync();
		}

		public async Task<IList<Article>> List(PageModule pageModule)
		{
			return await this.Context.Articles
				.Where(article => EF.Property<Guid>(article, "ModuleId") == pageModule.Id)
				.Include(article => article.Categories)
					.ThenInclude(category => category.CategoryListItem)
				.Include(article => article.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(article => article.ImageFile)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();			
		}

		public async Task<PagedResult<Article>> List(PageModule pageModule, PagingSettings settings)
		{
			PagedResult<Article> results = new(settings);

			IQueryable<Article> query = this.Context.Articles
				.Where
				(
					article => EF.Property<Guid>(article, "ModuleId") == pageModule.Id &&
					article.Enabled &&
					(article.PublishDate == null || article.PublishDate.Value <= DateTime.UtcNow) &&
					(article.ExpireDate == null || article.ExpireDate.Value >= DateTime.UtcNow)
				);


			results.TotalCount = query.Count();

			results.Items = await query
				.OrderByDescending(article => article.Featured)
					.ThenByDescending(article => article.DateAdded)
				.Skip(settings.FirstRowIndex)
				.Take(settings.PageSize)
				.Include(article => article.ImageFile)
				.Include(article => article.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(article => article.Categories)
					.ThenInclude(category => category.CategoryListItem)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			return results;
		}

		private DateTime StartDate(Models.PublishedDateRanges range)
    {
			switch(range)
      {
				case PublishedDateRanges.LastWeek:
					return DateTime.UtcNow.AddDays(-7);

				case PublishedDateRanges.LastMonth:
					return DateTime.UtcNow.AddMonths(-1);

				case PublishedDateRanges.Last3Months:
					return DateTime.UtcNow.AddMonths(-3);

				case PublishedDateRanges.Last6Months:
					return DateTime.UtcNow.AddMonths(-6);

				case PublishedDateRanges.LastYear:
					return DateTime.UtcNow.AddYears(-1);

				case PublishedDateRanges.Last2Years:
					return DateTime.UtcNow.AddYears(-2);

				default:
					return DateTime.UtcNow;
			}
		}

		public async Task<PagedResult<Article>> List(PageModule pageModule, PagingSettings settings, FilterOptions filters)
		{
			PagedResult<Article> results = new(settings);

			IQueryable<Article> query = this.Context.Articles
				.Where
				(
					article => EF.Property<Guid>(article, "ModuleId") == pageModule.Id &&
					article.Enabled &&
					(article.PublishDate == null || article.PublishDate.Value <= DateTime.UtcNow) &&
					(article.ExpireDate == null || article.ExpireDate.Value >= DateTime.UtcNow) &&
					article.Categories.Where(category => filters.Categories.Select(item => item.Id).Contains(category.CategoryListItem.Id)).Any()
				);

			if (filters.FeaturedOnly)
			{
				query = query.Where(article => (article.Featured == true));
			}

			if (filters.PublishedDateRange != PublishedDateRanges.Any)
      {
				//query = query.Where(article => (article.PublishDate == null || article.PublishDate.Value <= StartDate(filters.PublishedDateRange)));
				query = query.Where(article => ((article.PublishDate == null ? article.DateAdded : article.PublishDate).Value >= StartDate(filters.PublishedDateRange)));
			}

			switch (filters.SortOrder)
      {
				case SortOrders.FeaturedAndDate:
					query = query
						.OrderByDescending(article => article.Featured)
						.ThenByDescending(article => article.PublishDate == null ? article.DateAdded : article.PublishDate);
					break;
				case SortOrders.Date:
					query = query.OrderByDescending(article => article.PublishDate == null ? article.DateAdded : article.PublishDate);
					break;
      }

			if (filters.PageSize != -1)
      {
				query = query.Take(filters.PageSize);
      }

			results.TotalCount = query.Count();

			results.Items = await query
				//.OrderByDescending(article => article.Featured)
				//	.ThenByDescending(article => article.DateAdded)
				.Skip(settings.FirstRowIndex)
				//.Take(settings.PageSize)
				.Include(article => article.ImageFile)
				.Include(article => article.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(article => article.Categories)
					.ThenInclude(category => category.CategoryListItem)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

				return results;
		}

		public async Task Save(PageModule pageModule, Article article)
		{
			Action raiseEvent;

			Boolean isNew = !this.Context.Articles.Where(existing => existing.Id == article.Id).Any();

			this.Context.Attach(article);
			this.Context.Entry(article).Property("ModuleId").CurrentValue = pageModule.Id;
			this.Context.Entry(article).Property("EncodedTitle").CurrentValue = article.Title.FriendlyEncode();

			if (isNew)
			{
				this.Context.Entry(article).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Article, Create>(article); });
			}
			else
			{
				this.Context.Entry(article).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Article, Update>(article); });
			}

			await this.Context.SaveChangesAsync<Article>();

			List<Attachment> currentAttachments = await this.Context.Attachments
				.Where(attachment => EF.Property<Guid>(attachment, "ArticleId") == article.Id)
				.AsNoTracking()
				.ToListAsync();

			await SaveAttachments(article.Id, article.Attachments, currentAttachments);

			List<Category> currentCategories = await this.Context.Categories
				.Where(attachment => EF.Property<Guid>(attachment, "ArticleId") == article.Id)
				.AsNoTracking()
				.ToListAsync();
			
			await SaveCategories(article.Id, article.Categories, currentCategories);

			raiseEvent.Invoke();
		}

		public async Task Delete(Article article)
		{
			this.Context.Remove(article);
			await this.Context.SaveChangesAsync<Article>();
		}

		private async Task SaveAttachments(Guid articleId, IEnumerable<Attachment> attachments, IEnumerable<Attachment> originalAttachments)
		{
			if (attachments == null) return;

			// delete removed attachments
			if (originalAttachments != null)
			{
				foreach (Attachment originalAttachment in originalAttachments)
				{
					Boolean found = false;

					foreach (Attachment newAttachment in attachments)
					{
						if (newAttachment.Id == originalAttachment.Id)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						this.Context.Attachments.Remove(originalAttachment);
					}
				}
			}

			// create added attachments
			foreach (Attachment attachment in attachments)
			{
				Boolean found = false;

				if (originalAttachments != null)
				{
					foreach (Attachment originalAttachment in originalAttachments)
					{
						if (attachment.Id == originalAttachment.Id)
						{
							found = true;
							break;
						}
					}
				}

				if (found)
				{
					// attachments are never updated
				}
				else
				{
					this.Context.Attachments.Add(attachment);
					this.Context.Entry(attachment).State = EntityState.Added;
					this.Context.Entry(attachment).Property("ArticleId").CurrentValue = articleId;					
				}
			}

			await this.Context.SaveChangesAsync<Attachment>();

		}

		private async Task SaveCategories(Guid articleId, IEnumerable<Category> categories, IEnumerable<Category> originalCategories)
		{
			if (categories == null) return;

			// delete removed categories
			if (originalCategories != null)
			{
				foreach (Category originalCategory in originalCategories)
				{
					Boolean found = false;

					foreach (Category newCategory in categories)
					{
						if (newCategory.Id == originalCategory.Id)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						this.Context.Categories.Remove(originalCategory);
					}
				}
			}

			// create added categories
			foreach (Category category in categories)
			{
				Boolean found = false;

				if (originalCategories != null)
				{
					foreach (Category originalCategory in originalCategories)
					{
						if (category.Id == originalCategory.Id)
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
					category.ArticleId = articleId;
					this.Context.Categories.Add(category);
					this.Context.Entry(category).State = EntityState.Added;					
				}
			}

			await this.Context.SaveChangesAsync<Category>();


		}

#endregion

		
  }
}
