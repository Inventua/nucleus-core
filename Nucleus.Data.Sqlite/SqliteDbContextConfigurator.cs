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

namespace Nucleus.Data.Sqlite
{	
	/// <summary>
	/// DbContext options configuration class for Sqlite
	/// </summary>
	/// <typeparam name="TDataProvider"></typeparam>
	public class SqliteDbContextConfigurator<TDataProvider> : DbContextConfigurator<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		private IOptions<DatabaseOptions> DatabaseOptions { get; }
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		/// <param name="folderOptions"></param>
		public SqliteDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
		{
			this.DatabaseOptions = databaseOptions;
			this.FolderOptions = folderOptions;
			this.DatabaseConnectionOption = this.DatabaseOptions.Value.GetDatabaseConnection(typeof(TDataProvider).GetDefaultSchemaName());			
		}

		/// <summary>
		/// Perform (or retry) pre-configuration checks.
		/// </summary>
		/// <returns></returns>
		public override Boolean PreConfigure()
		{
			// Special case for Sqlite - ensure that the folder exists
			FolderOptions folderOptions = this.FolderOptions.Value;

			Microsoft.Data.Sqlite.SqliteConnection connection = new(folderOptions.Parse(this.DatabaseConnectionOption.ConnectionString));

			try
			{
				if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(connection.DataSource)))
				{
					System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(connection.DataSource));
				}
				return true;
			}
			catch (System.UnauthorizedAccessException ex)
			{
				// permissions error on the Sqlite database folder.  Ignore here, and allow a database connection error
				// when Nucleus tries to connect to the database.
				return false;
			}
		}

		/// <summary>
		/// Configure the DbContextOptionsBuilder for Sqlite
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public override Boolean Configure(DbContextOptionsBuilder options)
		{
			if (this.DatabaseConnectionOption != null)
			{
				FolderOptions folderOptions = this.FolderOptions.Value;

				Microsoft.Data.Sqlite.SqliteConnection connection = new(folderOptions.Parse(this.DatabaseConnectionOption.ConnectionString));
				options.UseSqlite(connection.ConnectionString);

				return true;
			}

			return false;
		}		
	}
}
