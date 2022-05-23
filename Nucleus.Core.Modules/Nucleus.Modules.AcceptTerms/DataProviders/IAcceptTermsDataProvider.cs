using Nucleus.Abstractions.Models;
using Nucleus.Modules.AcceptTerms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.AcceptTerms.DataProviders
{
  public interface IAcceptTermsDataProvider : IDisposable
  {
    public Task<UserAcceptedTerms> Get(Guid moduleId, Guid userId);
    
    public Task Save(PageModule pageModule, UserAcceptedTerms acceptTerms);
    
    
  }
}
