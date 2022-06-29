using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.XmlDocumentation.Models;

namespace Nucleus.XmlDocumentation
{
	public static	class CacheExtensions
	{
		public static CacheCollection<Guid, List<ApiDocument>> XmlDocumentationCache (this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, List<ApiDocument>>();
		}
	}
}
