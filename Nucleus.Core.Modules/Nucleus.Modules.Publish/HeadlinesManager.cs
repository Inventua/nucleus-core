using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Publish.Models;
using Nucleus.Modules.Publish.DataProviders;

namespace Nucleus.Modules.Publish
{
  public class HeadlinesManager
  {
		private ICacheManager CacheManager { get; }

		private IDataProviderFactory DataProviderFactory { get; }

		public HeadlinesManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
    {
      this.DataProviderFactory = dataProviderFactory;
      this.CacheManager = cacheManager;
    }

    public async Task<FilterOptions> GetFilterOptions(PageModule pageModule)
    {
			return await this.CacheManager.HeadlinesFilterOptionsCache().GetAsync(pageModule.Id, async id =>
			{
				using (IHeadlinesDataProvider provider = this.DataProviderFactory.CreateProvider<IHeadlinesDataProvider>())
				{
					return await provider.GetFilterOptions(pageModule);
				}
			});
		}

		public async Task SaveFilterOptions(PageModule pageModule, FilterOptions options)
    {
			using (IHeadlinesDataProvider provider = this.DataProviderFactory.CreateProvider<IHeadlinesDataProvider>())
			{
				await provider.SaveFilterOptions(pageModule, options);
			}
		}

	}
}
