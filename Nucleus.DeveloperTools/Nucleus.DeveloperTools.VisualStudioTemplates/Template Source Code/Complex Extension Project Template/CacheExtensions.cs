using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using $nucleus_extension_namespace$.Models;

namespace $nucleus_extension_namespace$
{
	public static	class CacheExtensions
	{
		public static CacheCollection<Guid, $nucleus_extension_modelname$> $nucleus_extension_modelname$sCache(this ICacheManager cacheManager)
		{
			return cacheManager.Get<Guid, $nucleus_extension_modelname$> ();
		}
	}
}
