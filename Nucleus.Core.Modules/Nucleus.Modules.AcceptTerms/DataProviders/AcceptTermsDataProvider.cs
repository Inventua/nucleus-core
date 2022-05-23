using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.AcceptTerms.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.AcceptTerms.DataProviders
{
  /// <summary>
  /// Module data provider.
  /// </summary>
  /// <remarks>
  /// This class implements the IAcceptTermsDataProvider interface, and inherits the base Nucleus entity framework data provider class.
  /// </remarks>
  public class AcceptTermsDataProvider : Nucleus.Data.EntityFramework.DataProvider, IAcceptTermsDataProvider
  {
    protected IEventDispatcher EventManager { get; }
    protected new AcceptTermsDbContext Context { get; }

    public AcceptTermsDataProvider(AcceptTermsDbContext context, IEventDispatcher eventManager, ILogger<AcceptTermsDataProvider> logger) : base(context, logger)
    {
      this.EventManager = eventManager;
      this.Context = context;
    }

    public async Task<UserAcceptedTerms> Get(Guid moduleId, Guid userId)
    {
      return await this.Context.UserTermsAcceptance
        .Where(acceptTerm => EF.Property<Guid>(acceptTerm, "ModuleId") == moduleId && acceptTerm.UserId == userId)
        .AsNoTracking()
        .FirstOrDefaultAsync();
    }

    public async Task Save(PageModule pageModule, UserAcceptedTerms acceptTerm)
    {
      UserAcceptedTerms existing = await this.Context.UserTermsAcceptance
        .Where(existing => EF.Property<Guid>(existing, "ModuleId") == pageModule.Id && existing.UserId == acceptTerm.UserId)
        .FirstOrDefaultAsync();

      if (existing != null)
      {
        this.Context.Entry(existing).Property(existing => existing.DateAccepted).CurrentValue = acceptTerm.DateAccepted;
      }
      else
      {
        this.Context.Add(acceptTerm);
        this.Context.Entry(acceptTerm).Property<Guid>("ModuleId").CurrentValue = pageModule.Id;
      }

      await this.Context.SaveChangesAsync<UserAcceptedTerms>();
    }

  }
}