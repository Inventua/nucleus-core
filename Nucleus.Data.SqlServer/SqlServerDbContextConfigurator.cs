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
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Data.SqlServer
{
	/// <summary>
	/// DbContext options configuration class for Sqlite
	/// </summary>
	/// <typeparam name="TDataProvider"></typeparam>
	public class SqlServerDbContextConfigurator<TDataProvider> : DbContextConfigurator<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		private IOptions<DatabaseOptions> DatabaseOptions { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		public SqlServerDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions)
		{
			this.DatabaseOptions = databaseOptions;
			this.DatabaseConnectionOption = this.DatabaseOptions.Value.GetDatabaseConnection(typeof(TDataProvider).GetDefaultSchemaName());
		}

		/// <summary>
		/// Configure the DbContextOptionsBuilder for Sql server
		/// </summary>
		/// <param name="options"></param>
		public override Boolean Configure(DbContextOptionsBuilder options)
		{
			if (this.DatabaseConnectionOption != null)
			{
					options.UseSqlServer(this.DatabaseConnectionOption.ConnectionString);
					return true;
			}

			return false;
		}


	}
}
