using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Data provider configuration interface.
	/// </summary>
	public interface IDatabaseProvider
	{
		/// <summary>
		/// Database provider type key.
		/// </summary>
		/// <remarks>
		/// This value is used to represent the database provider in the database configuration file 'Type' property.
		/// </remarks>
		/// <returns></returns>
		public string TypeKey();
		
		/// <summary>
		/// Add data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses the database provider implementing this interface.  
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public IServiceCollection AddDataProvider<TDataProvider>(IServiceCollection services, DatabaseConnectionOption options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider;

		/// <summary>
		/// Return database diagnostics information if configuration contains an entry specifying that the data provider uses 
		/// the database provider implementing this interface.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public Dictionary<string, string> GetDatabaseInformation(IServiceProvider services, DatabaseConnectionOption options, string schemaName);
	}
}
