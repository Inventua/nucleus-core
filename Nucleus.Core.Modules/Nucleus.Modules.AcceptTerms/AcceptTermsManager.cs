using System.Security.Claims;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Modules.AcceptTerms.DataProviders;
using Nucleus.Modules.AcceptTerms.Models;
using Nucleus.Extensions.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.AcceptTerms
{
  /// <summary>
  /// Provides functions to manage database data for <see cref="AcceptTerms"/>s.
  /// </summary>
  public class AcceptTermsManager
  {
    private IDataProviderFactory DataProviderFactory { get; }
    private ICacheManager CacheManager { get; }

    public AcceptTermsManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
    {
      this.CacheManager = cacheManager;
      this.DataProviderFactory = dataProviderFactory;
    }

    /// <summary>
    /// Create a new <see cref="AcceptTerms"/> with default values.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    /// <remarks>
    /// The new <see cref="AcceptTerms"/> is not saved to the database until you call <see cref="Save(PageModule, AcceptTerms)"/>.
    /// </remarks>
    public UserAcceptedTerms CreateNew()
    {
      UserAcceptedTerms result = new();

      return result;
    }

    /// <summary>
    /// Retrieve an existing <see cref="UserAcceptedTerms"/> from the database.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<UserAcceptedTerms> Get(PageModule pageModule, ClaimsPrincipal user)
    {
      string key = CacheKey(pageModule, user.GetUserId());
      return await this.CacheManager.AcceptTermsCache().GetAsync(key, async id =>
      {
        using (IAcceptTermsDataProvider provider = this.DataProviderFactory.CreateProvider<IAcceptTermsDataProvider>())
        {
          return await provider.Get(pageModule.Id, user.GetUserId());
        }
      });
    }

    /// <summary>
    /// Create or update a <see cref="UserAcceptedTerms"/>.
    /// </summary>
    /// <param name="site"></param>
    /// <param name="AcceptTerms"></param>
    public async Task Save(PageModule module, ClaimsPrincipal user)
    {
      using (IAcceptTermsDataProvider provider = this.DataProviderFactory.CreateProvider<IAcceptTermsDataProvider>())
      {
        await provider.Save(module, new() 
        { 
           UserId = user.GetUserId(),
           DateAccepted = DateTime.UtcNow
        });

        this.CacheManager.AcceptTermsCache().Remove(CacheKey(module, user.GetUserId()));
      }
    }

    private string CacheKey(PageModule module, Guid userId)
    {
      return $"{module.Id}|{userId}";
    }
  }
}
