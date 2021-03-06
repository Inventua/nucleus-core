using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core
{
	public static class CacheManagerExtensions
	{
		public static CacheCollection<string, PageMenu> PageMenuCache (this ICacheManager cacheManager)  { return cacheManager.Get<string, PageMenu>(); } 
		public static CacheCollection<Guid, Page> PageCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, Page>(); }
		public static CacheCollection<string, Page> PageRouteCache(this ICacheManager cacheManager) { return cacheManager.Get<string, Page>(); }

		public static CacheCollection<Guid, PageModule> PageModuleCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, PageModule>(); } 
		public static CacheCollection<Guid, RoleGroup> RoleGroupCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, RoleGroup>(); } 
		
		public static CacheCollection<Guid, Role> RoleCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, Role>(); }
		public static CacheCollection<Guid, UserSession> SessionCache(this ICacheManager cacheManager) { return cacheManager.Get<Guid, UserSession>(); }

		public static CacheCollection<Guid, Site> SiteCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, Site>(); }
		public static CacheCollection<string, Site> SiteDetectCache(this ICacheManager cacheManager) { return cacheManager.Get<string, Site>(); }

		public static CacheCollection<Guid, User> UserCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, User>(); } 
		public static CacheCollection<Guid, MailTemplate> MailTemplateCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, MailTemplate>(); } 

		public static CacheCollection<Guid, ScheduledTask> ScheduledTaskCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, ScheduledTask>(); } 
		public static CacheCollection<Guid, Folder> FolderCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, Folder>(); } 
		public static CacheCollection<Guid, SiteGroup> SiteGroupCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, SiteGroup>(); } 
		public static CacheCollection<Guid, List> ListCache (this ICacheManager cacheManager)  { return cacheManager.Get<Guid, List>(); }

		public static CacheCollection<Guid, Content> ContentCache(this ICacheManager cacheManager) { return cacheManager.Get<Guid, Content>(); }
		public static CacheCollection<Guid, ApiKey> ApiKeyCache(this ICacheManager cacheManager) { return cacheManager.Get<Guid, ApiKey>(); }

	}
}
