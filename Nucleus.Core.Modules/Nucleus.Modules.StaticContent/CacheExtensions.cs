using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using System;
using Nucleus.Modules.StaticContent.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.StaticContent
{
	public static class CacheExtensions
	{
		internal static CacheCollection<String, RenderedContent> StaticContentCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<String, RenderedContent>();
		}
	}
}
