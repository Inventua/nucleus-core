using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Data.Common;

namespace Nucleus.Data.Sqlite
{
	/// <summary>
	/// 
	/// </summary>
	public class SqliteProvider : IConfigureDataProvider
	{
		/// <summary>
		/// Add Sqlite data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses Sqlite.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public Boolean AddDataProvider<TDataProvider>(IServiceCollection services, DatabaseOptions options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			return AddSqlite<TDataProvider>(services, options, schemaName);
		}

		/// <summary>
		/// Add Sqlite data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses Sqlite.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		private Boolean AddSqlite<TDataProvider>(IServiceCollection services, DatabaseOptions options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			// Get connection for the specified schema name.  If it is found, add Sqlite data provider objects to the services collection.
			DatabaseConnectionOption connectionOption = options.GetDatabaseConnection(schemaName);

			if (connectionOption != null && connectionOption.Type == "Sqlite")
			{
				services.AddTransient<Nucleus.Data.Common.DataProviderMigration<TDataProvider>, Nucleus.Data.Sqlite.SqliteDataProviderMigration<TDataProvider>>();
				services.AddSingleton<Nucleus.Data.EntityFramework.DbContextConfigurator<TDataProvider>, Nucleus.Data.Sqlite.SqliteDbContextConfigurator<TDataProvider>>();
				return true;
			}

			return false;
		}
	}
}
