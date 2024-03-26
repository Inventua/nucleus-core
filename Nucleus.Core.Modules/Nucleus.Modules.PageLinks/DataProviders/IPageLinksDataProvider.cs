using Nucleus.Abstractions.Models;
using Nucleus.Modules.PageLinks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.PageLinks.DataProviders;

public interface IPageLinksDataProvider : IDisposable
{
  //public Task<PageLink> Get(Guid Id);
  public Task<List<PageLink>> List(PageModule pageModule);
  public Task Save(PageModule pageModule, PageLink pageLink);
  //public Task Save(PageModule pageModule, List<PageLink> pageLinks);
  public Task Delete(PageLink pageLink);

}
