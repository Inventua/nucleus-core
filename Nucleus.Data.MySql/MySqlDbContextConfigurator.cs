using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nucleus.Data.EntityFramework;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Data.Common;

namespace Nucleus.Data.MySql
{
	/// <summary>
	/// DbContext options configuration class for Sqlite
	/// </summary>
	/// <typeparam name="TDataProvider"></typeparam>
	public class MySqlDbContextConfigurator<TDataProvider> : DbContextConfigurator<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		private IOptions<DatabaseOptions> DatabaseOptions { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		public MySqlDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions)
		{
			this.DatabaseOptions = databaseOptions;
		}

		/// <summary>
		/// Configure the DbContextOptionsBuilder for Sql server
		/// </summary>
		/// <param name="options"></param>
		public override Boolean Configure(DbContextOptionsBuilder options)
		{
			DatabaseConnectionOption connectionOption = this.DatabaseOptions.Value.GetDatabaseConnection(typeof(TDataProvider).GetDefaultSchemaName());

			if (connectionOption != null)
			{
					options.UseMySql(connectionOption.ConnectionString, ServerVersion.AutoDetect(connectionOption.ConnectionString));
					return true;
			}

			return false;
		}


	}
}
