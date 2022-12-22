using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core.DataProviders
{
  public interface IOrganizationDataProvider : IDisposable
  {
    abstract Task<IList<Organization>> ListOrganizations(Site site);
    abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<Organization>> ListOrganizations(Site site, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
    abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<Organization>> SearchOrganizations(Site site, string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
    abstract Task<Organization> GetOrganization(Guid OrganizationId);
    abstract Task<Organization> GetOrganizationByName(Site site, string name);
    
    abstract Task SaveOrganization(Site site, Organization organization);
    abstract Task DeleteOrganization(Organization organization);

    abstract Task AddOrganizationUser(OrganizationUser organizationUser);
    abstract Task RemoveOrganizationUser(OrganizationUser organizationUser);

    abstract Task<Boolean> IsOrganizationMember(Organization organization, Guid userId);
    abstract Task<Boolean> IsOrganizationAdministrator(Organization organization, Guid userId);
  }
}
