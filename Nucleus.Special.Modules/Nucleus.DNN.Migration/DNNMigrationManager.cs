using DocumentFormat.OpenXml.Office2010.CustomUI;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.DNN.Migration.DataProviders;
using Nucleus.DNN.Migration.MigrationEngines;
using Nucleus.DNN.Migration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration;
/// <summary>
/// Provides functions to manage database data.
/// </summary>
public class DNNMigrationManager
{
  private IDataProviderFactory DataProviderFactory { get; }

  private static List<MigrationEngineBase> CurrentOperationEngines { get; } = new();
  
  public DNNMigrationManager(IDataProviderFactory dataProviderFactory)
  {
    this.DataProviderFactory = dataProviderFactory;
    this.DataProviderFactory.PreventSchemaCheck(Startup.DNN_SCHEMA_NAME);    
  }

  public void ClearMigrateOperations()
  {
    if (!CurrentOperationEngines.All(engine => engine.Completed()))
    {
      throw new InvalidOperationException("A migration operation is already in progress.");
    }

    CurrentOperationEngines.Clear();
  }

  public Task Migrate<TModel>(IServiceProvider services, List<TModel> items)
    where TModel : Models.DNN.DNNEntity
  {
    if (!CurrentOperationEngines.All(engine => engine.Completed()))
    {
      throw new InvalidOperationException("A migration operation is already in progress.");
    }

    MigrationEngineBase<TModel> engine = services.CreateEngine<TModel>();
    CurrentOperationEngines.Add(engine);

    return engine.Migrate(items);
  }

  public List<MigrationEngineBase> CurrentOperations
  {
    get
    { 
      return CurrentOperationEngines;
    }
  }


  #region "    Migration History    "
  /// <summary>
  /// Create a new <see cref="Models.Migration"/> with default values.
  /// </summary>
  /// <param name="site"></param>
  /// <returns></returns>
  /// <remarks>
  /// The new <see cref="Models.Migration"/> is not saved to the database until you call <see cref="Save(PageModule, Models.Migration)"/>.
  /// </remarks>
  public Models.MigrationHistory CreateNew()
  {
    Models.MigrationHistory result = new();

    return result;
  }

  ///// <summary>
  ///// List all <see cref="Models.Migration"/>s within the specified site.
  ///// </summary>
  ///// <param name="module"></param>
  ///// <returns></returns>
  //public async Task<IList<Models.Migration>> List(PageModule module)
  //{
  //  using (IDNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<IDNNMigrationDataProvider>())
  //  {
  //    return await provider.List(module);
  //  }
  //}

  /// <summary>
  /// Create or update a <see cref="Models.Migration"/>.
  /// </summary>
  /// <param name="module"></param>
  /// <param name="Models.Migration"></param>
  public async Task Save(PageModule module, Models.MigrationHistory migrationHistory)
  {
    using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
    {
      await provider.Save(migrationHistory);      
    }
  }
  #endregion


  public async Task<Models.DNN.Version> GetDNNVersion()
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      // we must handle a null provider here, which will happen if there is no configured database schema with name "DNN".  The
      // main view shows a warning message when version = null
      if (provider == null) { return null; }

      return await provider.GetVersion();
    }
  }

  public async Task<List<Models.DNN.Portal>> ListDNNPortals()
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListPortals();
    }
  }

  public async Task<Models.DNN.RoleGroup> GetDNNRoleGroup(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetRoleGroup(id);
    }
  }

  public async Task<List<Models.DNN.RoleGroup>> ListDNNRoleGroups(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListRoleGroups(portalId);
    }
  }

  public async Task<Models.DNN.Role> GetDNNRole(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetRole(id);
    }
  }

  public async Task<List<Models.DNN.Role>> ListDNNRoles(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListRoles(portalId);
    }
  }

  public async Task<Models.DNN.User> GetDNNUser(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetUser(id);
    }
  }

  public async Task<List<Models.DNN.User>> ListDNNUsers(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListUsers(portalId);
    }
  }

  public async Task<Models.DNN.Page> GetDNNPage(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetPage(id);
    }
  }

  public async Task<List<Models.DNN.Page>> ListDNNPages(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListPages(portalId);
    }
  }
}
