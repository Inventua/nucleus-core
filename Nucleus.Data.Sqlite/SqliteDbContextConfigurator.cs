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
using static SQLitePCL.raw;
using System.Text.RegularExpressions;

namespace Nucleus.Data.Sqlite
{
	/// <summary>
	/// DbContext options configuration class for Sqlite
	/// </summary>
	/// <typeparam name="TDataProvider"></typeparam>
	public class SqliteDbContextConfigurator<TDataProvider> : DbContextConfigurator<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		/// <param name="folderOptions"></param>
		public SqliteDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions) : base(databaseOptions)
		{
			this.FolderOptions = folderOptions;
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
			catch (System.UnauthorizedAccessException)
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
				options.UseSqlite(connection.ConnectionString, options => 
				{
					// Sqlite locks the entire database during a write, so concurrent writes can cause SQLITE_BUSY ("Database is locked").  Entity
					// framework (actually Microsoft.Data.Sqlite) retries writes automatically until the time frame specified by .CommandTimeout.
					options.CommandTimeout(30);
				});

				return true;
			}

			return false;
		}

		/// <summary>
		/// Parse a database exception and match it to a friendly error message.  If a match is found, throw a <see cref="DataProviderException"/> to wrap the
		/// original exception with a more friendly error message.
		/// </summary>
		/// <param name="exception"></param>
		public override void ParseException(DbUpdateException exception)
		{
			// This code is inspired by, and uses code from https://github.com/Giorgi/EntityFramework.Exceptions.  https://www.apache.org/licenses/LICENSE-2.0
			if (exception == null) return;
			if (exception.InnerException == null) return;

			if (exception.InnerException is Microsoft.Data.Sqlite.SqliteException)
			{
				string message = "";

				Microsoft.Data.Sqlite.SqliteException dbException = exception.InnerException as Microsoft.Data.Sqlite.SqliteException;
					
				if (dbException.SqliteErrorCode ==  SQLITE_CONSTRAINT || dbException.SqliteErrorCode == SQLITE_TOOBIG)
				{
					switch (dbException.SqliteExtendedErrorCode)
					{
						case SQLITE_TOOBIG:
							// This error is generated when a blob is more than a million bytes.  This would typically be caused by a bug rather than user action, 
							// so we don't parse them.
							break;
						case SQLITE_CONSTRAINT_NOTNULL:
							message = ParseException(dbException, Messages.NOT_NULL_PATTERN, Messages.NOT_NULL_MESSAGE);
							break;
						case SQLITE_CONSTRAINT_UNIQUE:
							message = ParseException(dbException, Messages.UNIQUE_CONSTRAINT_PATTERN, Messages.UNIQUE_CONSTRAINT_MESSAGE);
							break;
						case SQLITE_CONSTRAINT_PRIMARYKEY:
							// primary key constraint failed errors would typically be caused by a bug rather than user action, so we don't parse them.
							break;
						case SQLITE_CONSTRAINT_FOREIGNKEY:
							// SQLite doesn't provide any useful information in the error message when a foreign key constraint fails, so the parsing for this
							// isn't very useful - it just generates an exception with message 'FOREIGN KEY constraint failed'.  Foreign key
							// constraint errors are not normally caused by a user action, so this should not be a serious issue.
							message = ParseException(dbException, Messages.FOREIGN_KEY_PATTERN, Messages.FOREIGN_KEY_MESSAGE);
							break;
					};

					if (!String.IsNullOrEmpty(message))
					{
						throw new Nucleus.Abstractions.DataProviderException(message, exception);
					}
				}
			}
		}
		
		internal class Messages
		{
			internal const string UNIQUE_CONSTRAINT_PATTERN = @"'UNIQUE constraint failed: (?<columns>.*)'.";
			internal const string UNIQUE_CONSTRAINT_MESSAGE = @"The combination of {columns} must be unique.";

			internal const string NOT_NULL_PATTERN = @"NOT NULL constraint failed: (?<table>.*)\.(?<column>.*)'";
			internal const string NOT_NULL_MESSAGE = "The '{column}' field is required.";

			internal const string FOREIGN_KEY_PATTERN = @"FOREIGN KEY constraint failed.";
			internal const string FOREIGN_KEY_MESSAGE = @"FOREIGN KEY constraint failed.";
		}
	}
}
