using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.Common;

namespace Nucleus.Data.Common
{
  /// <summary>
  /// Data provider factory base class.  Provides common functionality for data providers.
  /// </summary>
  public class DataProviderFactory : IDataProviderFactory
  {
    private ILogger<DataProviderFactory> Logger { get; }
    private IServiceProvider RequestServices { get; }
    private Dictionary<string, Boolean> IsSchemaChecked { get; } = new();
    private Dictionary<System.Type, Boolean> IsConnectionChecked { get; } = new();

    private static readonly object lockObject = new();

    /// <summary>
    /// Constructor (called by DI)
    /// </summary>
    public DataProviderFactory(IServiceProvider requestServices, ILogger<DataProviderFactory> logger)
    {
      this.RequestServices = requestServices;
      this.Logger = logger;
    }

    /// <summary>
    /// Prevent database schema checking.  
    /// </summary>
    /// <param name="schemaName"></param>
    /// <remarks>
    /// Extensions can use this method to suppress the default Nucleus behaviour of checking for a Schemas table, comparing versions, and 
    /// running database schema update scripts.  This is for cases where an extension is using a database from another system, and should
    /// not attempt to manage the database schema.
    /// </remarks>
    public void PreventSchemaCheck(string schemaName)
    {
      lock (lockObject)
      {
        if (!this.IsSchemaChecked.ContainsKey(schemaName))
        {
          this.IsSchemaChecked.Add(schemaName, true);
        }
      }
    }

    /// <summary>
    /// Create a new instance of the specified data provider interface, after checking whether migration scripts need to be applied.
    /// </summary>
    /// <typeparam name="TDataProvider"></typeparam>
    /// <returns>A data provider instance of the specified type.</returns>
    /// <remarks>
    /// If migration scripts exist which have not been applied, scripts are automatically executed before returning the data provider object.  Migration 
    /// script checks are not run again until after a restart. 
    /// </remarks>
    public TDataProvider CreateProvider<TDataProvider>()
    {
      TDataProvider provider = (TDataProvider)this.RequestServices.GetService<TDataProvider>();

      if (provider != null)
      {
        Boolean checkConnectionRequired = (!this.IsConnectionChecked.ContainsKey(provider.GetType()) || !this.IsConnectionChecked[provider.GetType()]);

        if (checkConnectionRequired)
        {
          lock (lockObject)
          {
            if (!this.IsConnectionChecked.ContainsKey(provider.GetType()) || !this.IsConnectionChecked[provider.GetType()])
            {
              DataProvider baseProvider = provider as DataProvider;

              if (baseProvider != null)
              {
                baseProvider.CheckConnection();
              }

              this.IsConnectionChecked.Add(provider.GetType(), true);
            }
          }
        }

       // There doesn't have to be a DataProviderMigration<T> in the services collection, but if there is, we can
        // check for new schema updates and perform schema migration.
        string schemaName = DataProviderSchemas.GetSchemaName(provider.GetType()).DataProviderSchemaName;

        Boolean checkSchemaRequired = (!this.IsSchemaChecked.ContainsKey(schemaName) || !this.IsSchemaChecked[schemaName]);

        if (checkSchemaRequired)
        { 
          DataProviderMigration migration = (DataProviderMigration)this.RequestServices.GetService(typeof(DataProviderMigration<>).MakeGenericType(new Type[] { provider.GetType() }));
        
          if (migration != null)
          {
            lock (lockObject)
            {
              if (!this.IsSchemaChecked.ContainsKey(schemaName) || !this.IsSchemaChecked[schemaName])
              {
                Logger.LogInformation("Checking database schema [{schemaName}].", schemaName);
                migration.CheckDatabaseSchema();

                if (this.IsSchemaChecked.ContainsKey(schemaName))
                {
                  this.IsSchemaChecked[schemaName] = true;
                }
                else
                {
                  this.IsSchemaChecked.Add(schemaName, true);
                }
              }
            }
          }
        }
      }

      return provider;
    }
  }
}
