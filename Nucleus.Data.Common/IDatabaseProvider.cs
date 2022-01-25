using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Data provider configuration interface.
	/// </summary>
	public interface IDatabaseProvider
	{
		/// <summary>
		/// Add data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses the database provider implementing this interface.  
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public Boolean AddDataProvider<TDataProvider>(IServiceCollection services, DatabaseOptions options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider;

		/// <summary>
		/// Return database diagnostics information if configuration contains an entry specifying that the data provider uses 
		/// the database provider implementing this interface.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public Dictionary<string, string> GetDatabaseInformation(DatabaseOptions options, string schemaName);
	}
}
