using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.Cache;
using Microsoft.Extensions.Options;

namespace Nucleus.Core
{
	public class CacheManager
	{
		private Dictionary<string, ICacheCollection> Caches { get; } = new();

		public CacheCollection<string, PageMenu> PageMenuCache { get { return Get<string, PageMenu>(); } }
		public CacheCollection<Guid, Page> PageCache { get { return Get<Guid, Page>(); } }
		public CacheCollection<Guid, PageModule> PageModuleCache { get { return Get<Guid, PageModule>(); } }
		public CacheCollection<Guid, RoleGroup> RoleGroupCache { get { return Get<Guid, RoleGroup>(); } }
		public CacheCollection<Guid, Role> RoleCache { get { return Get<Guid, Role>(); } }
		public CacheCollection<Guid, Site> SiteCache { get { return Get<Guid, Site>(); } }
		public CacheCollection<string, Guid?> SiteDetectCache { get { return Get<string, Guid?>(); } }
		public CacheCollection<Guid, User> UserCache { get { return Get<Guid, User>(); } }
		public CacheCollection<Guid, MailTemplate> MailTemplateCache { get { return Get<Guid, MailTemplate>(); } }
		public CacheCollection<Guid, ScheduledTask> ScheduledTaskCache { get { return Get<Guid, ScheduledTask>(); } }
		public CacheCollection<Guid, Folder> FolderCache { get { return Get<Guid, Folder>(); } }
		public CacheCollection<Guid, SiteGroup> SiteGroupCache { get { return Get<Guid, SiteGroup>(); } }

		public CacheCollection<Guid, List> ListCache { get { return Get<Guid, List>(); } }

		public CacheManager(IOptions<CacheOptions> options)
		{
			Add<Guid, Page>(options.Value.PageCache);
			Add<Guid, PageModule>(options.Value.PageModuleCache);
			Add<Guid, RoleGroup>(options.Value.RoleGroupCache);
			Add<Guid, Role>(options.Value.RoleCache);

			Add<Guid, SiteGroup>(options.Value.SiteGroupCache);
			Add<Guid, Site>(options.Value.SiteCache);
			Add<string, Guid?>(options.Value.SiteDetectCache);
			Add<Guid, User>(options.Value.UserCache);
			Add<Guid, MailTemplate>(options.Value.MailTemplateCache);

			Add<Guid, ScheduledTask>(options.Value.ScheduledTaskCache);
			Add<Guid, Folder>(options.Value.FolderCache);
			Add<string, PageMenu>(options.Value.PageMenuCache);

			Add<Guid, List>(options.Value.ListCache);
		}

		private static string CacheKey<TKey, TModel>()
		{
			return $"{typeof(TKey).FullName} {typeof(TModel).FullName}";
		}

		public CacheCollection<TKey, TModel> Add<TKey, TModel>(CacheOption options)
		{
			CacheCollection<TKey, TModel> result;

			if (!this.Caches.ContainsKey(CacheKey<TKey, TModel>()))
			{
				result = new(options);
				this.Caches.Add(CacheKey<TKey, TModel>(), result);
			}
			else
			{
				result = this.Caches[CacheKey<TKey, TModel>()] as CacheCollection<TKey, TModel>;
			}

			return result;
		}

		public CacheCollection<TKey, TModel> Get<TKey, TModel>()
		{
			if (this.Caches.TryGetValue(CacheKey<TKey, TModel>(), out ICacheCollection result))
			{
				if (result is CacheCollection<TKey, TModel>)
				{
					return result as CacheCollection<TKey, TModel>;
				}
			}

			return null;
		}

	}
}
