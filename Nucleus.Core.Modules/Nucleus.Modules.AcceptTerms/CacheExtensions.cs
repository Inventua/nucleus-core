using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Cache;
using Nucleus.Modules.AcceptTerms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.AcceptTerms
{
  public static class CacheExtensions
  {
    public static CacheCollection<string, UserAcceptedTerms> AcceptTermsCache(this ICacheManager cacheManager)
    {
      return cacheManager.Get<string, UserAcceptedTerms>();
    }
  }
}
