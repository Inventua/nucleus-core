using System;
using Nucleus.Data.Common;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Base class used by data providers.
	/// </summary>
	/// <remarks>
	/// Nucleus core and module data provider classes inherit this class.
	/// </remarks>
	public abstract class DataProvider : IDisposable
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public DataProvider()
		{
		}

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
		/// <param name="services"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		/// <remarks>
		/// The logic to check whether a specific database provider is the right one to use for the specified schema name 
		/// is implemented within each <see cref="IDatabaseProvider"/> implementation.
		/// </remarks>
		public Dictionary<string, string> GetDatabaseInformation(IServiceProvider services, string schemaName)
		{
			return DataProviderExtensions.GetDataProviderInformation(services, schemaName);			
		}

		/// <summary>
		/// Get a "friendly" name for the database.
		/// </summary>
		/// <returns></returns>
		public abstract string GetDatabaseKey();

		/// <summary>
		/// Dispose of resources.
		/// </summary>
		public virtual void Dispose()
		{

		}
	}

}
