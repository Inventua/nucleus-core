using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Data provider factory base class.  A data provider factory retrieves data provider interface instances from the dependency injection container.
	/// </summary>
	public interface IDataProviderFactory
	{
		/// <summary>
		/// Create a new instance of the specified data provider interface
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <returns>A data provider instance of the specified type.</returns>
		public TDataProvider CreateProvider<TDataProvider>();

    /// <summary>
    /// Prevent database schema checking.  
    /// </summary>
    /// <param name="schemaName"></param>
    /// <remarks>
    /// Extensions can use this method to suppress the default Nucleus behaviour of checking for a Schemas table, comparing versions, and 
    /// running database schema update scripts.  This is for cases where an extension is using a database from another system, and should
    /// not attempt to manage the database schema.
    /// </remarks>
    public void PreventSchemaCheck(string schemaName);
  }
}
