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

namespace Nucleus.Data.PostgreSql
{
	/// <summary>
	/// DbContext options configuration class for PostgreSql
	/// </summary>
	/// <typeparam name="TDataProvider"></typeparam>
	public class PostgreSqlDbContextConfigurator<TDataProvider> : DbContextConfigurator<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		private IOptions<DatabaseOptions> DatabaseOptions { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		public PostgreSqlDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions)
		{
			this.DatabaseOptions = databaseOptions;
			this.DatabaseConnectionOption = this.DatabaseOptions.Value.GetDatabaseConnection(typeof(TDataProvider).GetDefaultSchemaName());			
		}

		/// <summary>
		/// Configure the DbContextOptionsBuilder for PostgreSQL
		/// </summary>
		/// <param name="options"></param>
		public override Boolean Configure(DbContextOptionsBuilder options)
		{
			if (this.DatabaseConnectionOption != null)
			{
					options.UseNpgsql(this.DatabaseConnectionOption.ConnectionString);
					return true;
			}

			return false;
		}


	}
}
