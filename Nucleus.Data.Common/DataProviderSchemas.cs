using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Data.Common;

/// <summary>
/// Class used to store the schema name which was used to create a data provider for later reference.
/// </summary>
public class DataProviderSchemas
{
  private static ConcurrentDictionary<System.Type, SchemaName> Schemas = new();

  /// <summary>
  /// Store the schema name which was used to create a data provider for later reference.  If a schema name is 
  /// </summary>
  /// <param name="type"></param>
  /// <param name="providerSchemaName"></param>
  /// <param name="resolvedSchemaName"></param>
  public static void RegisterSchema(System.Type type, string providerSchemaName, string resolvedSchemaName)
  {
    Schemas.TryAdd(type, new SchemaName (providerSchemaName, resolvedSchemaName));
  }

  /// <summary>
  /// Return the Nucleus database schema name for the specified type.
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static SchemaName GetSchemaName(System.Type type)
  {
    if (Schemas.ContainsKey(type)) 
    { 
      return Schemas[type];
    }
    return null;
  }

  /// <summary>
  /// Class used to store a data provider schema name and the resolved schema name from configuration.
  /// </summary>
  public class SchemaName
  {
    internal SchemaName(string dataProviderSchemaName, string resolvedSchemaName)
    {
      this.DataProviderSchemaName=dataProviderSchemaName;
      this.ResolvedSchemaName =resolvedSchemaName;  
    }

    /// <summary>
    /// Original schema name for the data provider.
    /// </summary>
    public string DataProviderSchemaName { get; }

    /// <summary>
    /// Schema name from configuration that is used at run time.
    /// </summary>
    public string ResolvedSchemaName { get; }

  }
}
