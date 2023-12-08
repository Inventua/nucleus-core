using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;

namespace Nucleus.Data.Common;


/// <summary>
/// Common data provider dependency injection registration extension methods.
/// </summary>
public static class DataProviderExtensions
{
  /// <summary>
  /// Cache for list of types which implement IDatabaseProvider.  These are cached so that we don't iterate assemblies and types multiple times 
  /// during startup (every time AddDataProvider is called).
  /// </summary>
  private static IEnumerable<Type> _cachedDatabaseProviders;

  /// <summary>
  /// Find IDatabaseProvider implementations.
  /// </summary>
  /// <returns></returns>
  static IEnumerable<Type> GetDatabaseProviders()
  {
    if (_cachedDatabaseProviders == null)
    {
      _cachedDatabaseProviders = System.Runtime.Loader.AssemblyLoadContext.All
      .SelectMany(context => context.Assemblies)
      .SelectMany(assm => GetTypes(assm)
        .Where(type => typeof(IDatabaseProvider).IsAssignableFrom(type) && !type.Equals(typeof(IDatabaseProvider))));
    }

    return _cachedDatabaseProviders;
  }

  /// <summary>
  /// Add data provider objects to the service collection for the data provider specified by TDataProvider.  This overload uses the default 
  /// schema name for the data provider type.
  /// </summary>
  /// <typeparam name="TDataProvider"></typeparam>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  /// <remarks>
  /// Data providers which are based on Nucleus.Data.EntityFramework do not need to call this directly - it is called by the
  /// Nucleus.Data.EntityFramework.DataProviderExtensions.AddDataProvider() methods.
  /// </remarks>
  static public IServiceCollection AddDataProvider<TDataProvider>(this IServiceCollection services, IConfiguration configuration)
    where TDataProvider : Nucleus.Data.Common.DataProvider
  {
    string schemaName = typeof(TDataProvider).GetDefaultSchemaName();
    return AddDataProvider<TDataProvider>(services, configuration, schemaName, true);
  }

  /// <summary>
  /// Add data provider objects to the service collection for the data provider specified by TDataProvider.  This overload allows callers 
  /// to specify their schema name instead of using the default.
  /// </summary>
  /// <typeparam name="TDataProvider"></typeparam>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="schemaName"></param>
  /// <returns></returns>
  /// <remarks>
  /// This overload is used when developers want to specify their schema name instead of using the default.
  /// </remarks>
  static public IServiceCollection AddDataProvider<TDataProvider>(this IServiceCollection services, IConfiguration configuration, string schemaName)
  where TDataProvider : Nucleus.Data.Common.DataProvider
  {
    return AddDataProvider<TDataProvider>(services, configuration, schemaName, false);
  }

  /// <summary>
  /// Add data provider objects to the service collection for the data provider specified by TDataProvider.  This overload allows callers 
  /// to specify their schema name instead of using the default.
  /// </summary>
  /// <typeparam name="TDataProvider"></typeparam>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="schemaName"></param>
  /// <param name="canUseDefault"></param>
  /// <returns></returns>
  /// <remarks>
  /// This overload is used when developers want to specify their schema name, but want Nucleus to "fall back" to using the default.
  /// </remarks>
  static private IServiceCollection AddDataProvider<TDataProvider>(this IServiceCollection services, IConfiguration configuration, string schemaName, Boolean canUseDefault)
      where TDataProvider : Nucleus.Data.Common.DataProvider
  {
    Boolean success = false;
    // We have to get database options ourselves (instead of using dependency injection) because dependency injection is not available yet 
    // when this function is called.
    DatabaseOptions options = new();
    configuration.GetSection(DatabaseOptions.Section).Bind(options, options => options.BindNonPublicProperties = true);

    // set isDatabaseConfigured before calling postconfigure so that we are checking the "raw" config data.  If the database is not
    // configured, we override the value of canUseDefault so that any modules that reqiure a specific db connection use the 
    // in-memory database created by PostConfigure() that is used to launch the setup wizard when the database hasnt been configured.
    Boolean isDatabaseConfigured = options.IsDatabaseConfigured();

    new ConfigureDatabaseOptions().PostConfigure("", options);

    // Get connection data for the specified schema name.  If it is found, add data provider objects to the services collection.
    DatabaseConnectionOption connectionOption = options.GetDatabaseConnection(schemaName, canUseDefault || !isDatabaseConfigured);
    string resolvedSchemaName = options.GetConfiguredSchema(schemaName, canUseDefault || !isDatabaseConfigured)?.Name;

    // Add data provider objects for the data provider specified by <T>.  The database provider required for <T> is specified in
    // config. 
    success = CreateDataProvider<TDataProvider>(services, connectionOption, schemaName, resolvedSchemaName);
    
    if (!success)
    {
      throw new InvalidOperationException($"Nucleus could not find a configured data provider for schema '{schemaName}'.");
    }

    return services;
  }

  private static Boolean CreateDataProvider<TDataProvider>(IServiceCollection services, DatabaseConnectionOption connectionOption, string requestedSchemaName, string resolvedSchemaName)
    where TDataProvider : Nucleus.Data.Common.DataProvider
  {
    foreach (Type implementation in GetDatabaseProviders())
    {
      if (Activator.CreateInstance(implementation) is IDatabaseProvider instance)
      {
        // Get connection data for the specified schema name.  If it is found, add data provider objects to the services collection.
        if (connectionOption?.Type.Equals(instance.TypeKey(), StringComparison.OrdinalIgnoreCase) == true)
        {
          instance.AddDataProvider<TDataProvider>(services, connectionOption, requestedSchemaName);

          // register the schema name
          Nucleus.Data.Common.DataProviderSchemas.RegisterSchema(typeof(TDataProvider), requestedSchemaName, resolvedSchemaName);
          return true;
        }
      }
    }

    return false;
  }


  /// <summary>
  /// Get database diagnostics information for the specified schema.
  /// </summary>
  /// <param name="options"></param>
  /// <param name="schemaName"></param>
  /// <returns></returns>
  static public Dictionary<string, string> GetDataProviderInformation(DatabaseOptions options, string schemaName)
  {
    Dictionary<string, string> results = new();

    // Determine which database provider services the specified schema name and retrieve database diagnostic information
    foreach (Type implementation in GetDatabaseProviders())
    {
      IDatabaseProvider instance = Activator.CreateInstance(implementation) as IDatabaseProvider;
      if (instance != null)
      {
        // Get connection for the specified schema name.  If it is found, get database information.
        DatabaseConnectionOption connectionOption = options.GetDatabaseConnection(schemaName);

        if (connectionOption != null && connectionOption.Type.Equals(instance.TypeKey(), StringComparison.OrdinalIgnoreCase))
        {
          foreach (KeyValuePair<string, string> value in instance.GetDatabaseInformation(connectionOption, schemaName))
          {
            results.Add(value.Key, value.Value);
          }

          break;
        }
      }
    }

    return results;
  }

  private static Type[] GetTypes(System.Reflection.Assembly assembly)
  {
    try
    {
      return assembly.GetTypes();
    }
    catch (System.Reflection.ReflectionTypeLoadException)
    {
      return Array.Empty<Type>();
    }
  }

  /// <summary>
  /// Add the default data provider factory to the services collection.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  public static IServiceCollection AddDataProviderFactory(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);
    services.ConfigureOptions<ConfigureDatabaseOptions>();

    services.AddSingleton<IDataProviderFactory, DataProviderFactory>();
    return services;
  }

  /// <summary>
  /// Clear cache after startup to save memory.
  /// </summary>
  /// <param name="services"></param>
  public static void CleanupDataProviderExtensions(this IServiceCollection services)
  {
    _cachedDatabaseProviders = null;
  }

  /// <summary>
  /// Return the default namespace for a type.
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  /// <remarks>
  /// The specified type would typically be a data provider.  The default schema name is derived by returning the part of the namespace 
  /// before ".DataProvider".  If the namespace does not contain ".DataProvider", this function returns an empty string.  The convention
  /// for modules is to us a namespace with ".DataProviders" (plural), but using ".DataProvider" (singular) as the search value handles
  /// both plural and singular cases.
  /// </remarks>
  private static string GetDefaultSchemaName(this System.Type type)
  {
    string typeNamespace = type.Namespace;

    // set the default schema name to the namespace part before ".DataProvider" if the namespace container ".DataProvider"
    if (typeNamespace.Contains(".DataProvider", StringComparison.OrdinalIgnoreCase))
    {
      return typeNamespace.Substring(0, typeNamespace.LastIndexOf(".DataProvider", StringComparison.OrdinalIgnoreCase));
    }

    return "";
  }

}

/// <summary>
/// Check and post-configure database options
/// </summary>
/// <internal />
public class ConfigureDatabaseOptions : IPostConfigureOptions<DatabaseOptions>
{
  /// <internal />
  public void PostConfigure(string name, DatabaseOptions options)
  {
    if (!options.IsDatabaseConfigured())
    {
      // if the database is not configured yet, create the data provider using an in-memory SQLite database
      // so that Nucleus can run and redirect to the setup wizard on first run.

      // create an auto-generated temporary-use connection option and add to config
      DatabaseConnectionOption connectionOption = new()
      {
        Key = $"InMemorySqlite_{Guid.NewGuid()}",
        Type = "Sqlite",
        ConnectionString = "Data Source=file::memory:;Cache=Shared"
      };
      options.Connections.Add(connectionOption);
      options.Schemas.Add(new() { Name = "*", ConnectionKey = connectionOption.Key });      
    }
  }
}