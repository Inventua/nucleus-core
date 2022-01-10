using Nucleus.Abstractions.Models.Cache;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Links.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Links
{
    public static class CacheExtensions
    {
        public static CacheCollection<Guid, Link> LinksCache(this ICacheManager cacheManager)
        {
            return cacheManager.Get<Guid, Link>();
        }
    }
}
