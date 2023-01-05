using DocumentFormat.OpenXml.Office2010.Excel;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders;
using Nucleus.Data.Common;
using Nucleus.Extensions.Authorization;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core.Managers
{
  public class ExtensionsStoreManager : IExtensionsStoreManager
  {
    private IDataProviderFactory DataProviderFactory { get; }

    public ExtensionsStoreManager(IDataProviderFactory dataProviderFactory)
    {
      this.DataProviderFactory = dataProviderFactory;
    }

    public Task<ExtensionsStoreSettings> CreateNew(string storeUrl, ClaimsPrincipal user)
    {
      return Task.FromResult(new ExtensionsStoreSettings()
      {
        RegistrationDate = DateTime.UtcNow,
        RegisteredBy = user.GetUserId(),
        StoreUri = storeUrl,
        Track = ExtensionsStoreSettings.PackageTracks.Standard
      });
    }

    public async Task<ExtensionsStoreSettings> Get(string storeUrl)
    {
      using (IExtensionsStoreDataProvider provider = this.DataProviderFactory.CreateProvider<IExtensionsStoreDataProvider>())
      {
        return await provider.GetExtensionsStoreSettings(storeUrl);
      }
    }

    public async Task Save(ExtensionsStoreSettings settings)
    {
      using (IExtensionsStoreDataProvider provider = this.DataProviderFactory.CreateProvider<IExtensionsStoreDataProvider>())
      {
        await provider.SaveExtensionsStoreSettings(settings);
      }
    }

  }
}