using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.DNN.Migration.DataProviders;
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

  public DNNMigrationManager(IDataProviderFactory dataProviderFactory)
  {
    this.DataProviderFactory = dataProviderFactory;
    this.DataProviderFactory.PreventSchemaCheck(Startup.DNN_SCHEMA_NAME);    
  }

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

  /// <summary>
  /// Create a new <see cref="Models.Migration"/> with default values.
  /// </summary>
  /// <param name="site"></param>
  /// <returns></returns>
  /// <remarks>
  /// The new <see cref="Models.Migration"/> is not saved to the database until you call <see cref="Save(PageModule, Models.Migration)"/>.
  /// </remarks>
  public Models.MigrationLog CreateNew()
  {
    Models.MigrationLog result = new();

    return result;
  }

  /// <summary>
  /// Retrieve an existing <see cref="Models.Migration"/> from the database.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public async Task<Models.MigrationLog> Get(Guid id)
  {    
    using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
    {
      return await provider.Get(id);
    }
   
  }

  /// <summary>
  /// Delete the specified <see cref="Models.Migration"/> from the database.
  /// </summary>
  /// <param name="Models.Migration"></param>
  public async Task Delete(Models.MigrationLog migrationLog)
  {
    using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
    {
      await provider.Delete(migrationLog);
    }
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
  public async Task Save(PageModule module, Models.MigrationLog migrationLog)
  {
    using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
    {
      await provider.Save(migrationLog);      
    }
  }

}
