using System;
using Nucleus.Data.Common;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Base class used by database providers.
	/// </summary>
	/// <remarks>
	/// Nucleus database provider classes inherit this class.  Modules generally inherit Nucleus.Data.EntityFramework.DataProvider instead.
	/// </remarks>
	public abstract class DataProvider : IDisposable
	{
    /// <summary>
    /// Check the database connection.  
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// This method will throw a database provider-specific exception on error.
    /// </remarks>
    abstract public void CheckConnection();

    /// <summary>
    /// Get database diagnostics information for the specified schema.
    /// </summary>
    /// <param name="databaseOptions"></param>
    /// <param name="schemaName"></param>
    /// <returns></returns>
    /// <remarks>
    /// The logic to check whether a specific database provider is the right one to use for the specified schema name 
    /// is implemented within each <see cref="IDatabaseProvider"/> implementation.
    /// </remarks>
    public Dictionary<string, string> GetDatabaseInformation(DatabaseOptions databaseOptions, string schemaName)
		{
			return DataProviderExtensions.GetDataProviderInformation(databaseOptions, schemaName);			
		}

		/// <summary>
		/// Get the configuration key for the database.
		/// </summary>
		/// <returns></returns>
		public abstract string GetDatabaseKey();

		/// <summary>
		/// Dispose of resources.
		/// </summary>
		public virtual void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}

}
