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

namespace Nucleus.Data.PostgreSql
{
	/// <summary>
	/// 
	/// </summary>
	public class PostgreSqlProvider : IConfigureDataProvider
	{
		/// <summary>
		/// Add SqlServer data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses SqlServer.  This overload allows callers to specify their schema name instead 
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
			return AddPostgreSql<TDataProvider>(services, options, schemaName);
		}

		/// <summary>
		/// Add SqlServer data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses SqlServer.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		static private Boolean AddPostgreSql<TDataProvider>(IServiceCollection services, DatabaseOptions options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			// Get connection for the specified schema name.  If it is found, add PostgreSQL data provider objects to the services collection.
			DatabaseConnectionOption connectionOption = options.GetDatabaseConnection(schemaName);

			if (connectionOption != null && connectionOption.Type == "PostgreSql")
			{
				services.AddTransient<Nucleus.Data.Common.DataProviderMigration<TDataProvider>, Nucleus.Data.PostgreSql.PostgreSqlDataProviderMigration<TDataProvider>>();
				services.AddSingleton<Nucleus.Data.EntityFramework.DbContextConfigurator<TDataProvider>, Nucleus.Data.PostgreSql.PostgreSqlDbContextConfigurator<TDataProvider>>();
				return true;
			}

			return false;
		}


	}
}
