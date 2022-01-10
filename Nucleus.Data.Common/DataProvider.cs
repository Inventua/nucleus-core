using System;
using Nucleus.Data.Common;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using System.Linq;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Base class used by entity-framework data providers.
	/// </summary>
	/// <remarks>
	/// Nucleus core and module data provider which use entity framework classes inherit this class, which contains an implementation of
	/// the schema migration functions.  Data provider implementations which register a related DataProviderMigration class must inherit 
	/// this class.
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
		/// Dispose of resources.
		/// </summary>
		public virtual void Dispose()
		{
			
		}
	}

}
