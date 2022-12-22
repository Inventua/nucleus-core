using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Core.DataProviders;
using Nucleus.Data.Common;
using Nucleus.Extensions.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core.Managers
{
  public class OrganizationManager : IOrganizationManager
  {
    private IDataProviderFactory DataProviderFactory { get; }
    private ICacheManager CacheManager { get; }
    private ILogger<OrganizationManager> Logger { get; }

    public OrganizationManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, ILogger<OrganizationManager> logger)
    {
      this.DataProviderFactory = dataProviderFactory;
      this.CacheManager = cacheManager;
      this.Logger = logger;
    }

    public Task<Organization> CreateNew(Site site)
    {
      Organization result = new();
      result.Users = new();

      return Task.FromResult(result);
    }

    public async Task AddUser(Organization organization, OrganizationUser user)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        await provider.AddOrganizationUser(user);
        this.CacheManager.OrganizationCache().Remove(organization.Id);
      }
    }

    public async Task RemoveUser(Organization organization, OrganizationUser user)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        await provider.RemoveOrganizationUser(user);
        this.CacheManager.OrganizationCache().Remove(organization.Id);
      }
    }

    public async Task<Boolean> IsOrganizationMember(Organization organization, ClaimsPrincipal user)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        return await provider.IsOrganizationMember(organization, user.GetUserId()); 
      }
    }
    
    public async Task<Boolean> IsOrganizationAdministrator(Organization organization, ClaimsPrincipal user)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        return await provider.IsOrganizationAdministrator(organization, user.GetUserId());
      }
    }

    public async Task Delete(Organization organization)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        await provider.DeleteOrganization(organization);
        this.CacheManager.OrganizationCache().Remove(organization.Id);
      }
    }

    public async Task<Organization> Get(Guid id)
    {
      return await this.CacheManager.OrganizationCache().GetAsync(id, async id =>
      {
        using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
        {
          return await provider.GetOrganization(id);
        }
      });
    }

    public async Task<Organization> GetByName(Site site, string name)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        return await provider.GetOrganizationByName(site, name);
      }
    }

    public async Task<IList<Organization>> List(Site site)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        return await provider.ListOrganizations(site);
      }
    }

    public async Task<PagedResult<Organization>> List(Site site, PagingSettings pagingSettings)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        return await provider.ListOrganizations(site, pagingSettings);
      }
    }

    public async Task Save(Site site, Organization organization)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        await provider.SaveOrganization(site, organization);
        this.CacheManager.OrganizationCache().Remove(organization.Id);
      }
    }

    public async Task<PagedResult<Organization>> Search(Site site, string searchTerm, PagingSettings pagingSettings)
    {
      using (IOrganizationDataProvider provider = this.DataProviderFactory.CreateProvider<IOrganizationDataProvider>())
      {
        return await provider.SearchOrganizations(site, searchTerm, pagingSettings);
      }
    }
  }
}
