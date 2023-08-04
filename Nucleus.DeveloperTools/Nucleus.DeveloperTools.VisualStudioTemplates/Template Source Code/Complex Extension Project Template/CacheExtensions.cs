using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using $nucleus.extension.namespace$.Models;

namespace $nucleus.extension.namespace$
{
	public static	class CacheExtensions
	{
		public static CacheCollection<Guid, $nucleus.extension.model_class_name$> $nucleus.extension.model_class_name$sCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, $nucleus.extension.model_class_name$> ();
		}
	}
}
