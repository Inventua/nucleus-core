using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nucleus.Data.EntityFramework;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Data.Common;

namespace Nucleus.Data.MySql
{
	/// <summary>
	/// DbContext options configuration class for MySql
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
			this.DatabaseConnectionOption = this.DatabaseOptions.Value.GetDatabaseConnection(typeof(TDataProvider).GetDefaultSchemaName());			
		}

		/// <summary>
		/// Configure the DbContextOptionsBuilder for MySql
		/// </summary>
		/// <param name="options"></param>
		public override Boolean Configure(DbContextOptionsBuilder options)
		{
			if (this.DatabaseConnectionOption != null)
			{
					options.UseMySql(this.DatabaseConnectionOption.ConnectionString, ServerVersion.AutoDetect(this.DatabaseConnectionOption.ConnectionString));
					return true;
			}

			return false;
		}


	}
}
