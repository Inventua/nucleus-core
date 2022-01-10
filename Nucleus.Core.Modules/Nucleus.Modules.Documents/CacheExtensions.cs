using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.Modules.Documents.Models;

namespace Nucleus.Modules.Documents
{
	public static	class CacheExtensions
	{
		public static CacheCollection<Guid, Document> DocumentCache (this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, Document>();
		}
	}
}
