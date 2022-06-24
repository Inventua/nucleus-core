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
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		public PostgreSqlDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions) : base(databaseOptions) { }

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

		/// <inheritdoc/>
		public override void ParseException(DbUpdateException exception)
		{
			// This code is inspired by, and uses code from https://github.com/Giorgi/EntityFramework.Exceptions.  https://www.apache.org/licenses/LICENSE-2.0
			if (exception.InnerException is Npgsql.NpgsqlException)
			{
				string message = "";

				Npgsql.NpgsqlException? dbException = exception.InnerException as Npgsql.NpgsqlException;

				if (dbException != null)
				{
					switch (dbException.SqlState)
					{
						case Npgsql.PostgresErrorCodes.ForeignKeyViolation:
							message = ParseException(dbException, Messages.FOREIGN_KEY_PATTERN, Messages.FOREIGN_KEY_MESSAGE);
							break;
						case Npgsql.PostgresErrorCodes.NotNullViolation:
							message = ParseException(dbException, Messages.NOT_NULL_PATTERN, Messages.NOT_NULL_MESSAGE);
							break;
						case Npgsql.PostgresErrorCodes.UniqueViolation:
							message = ParseException(dbException, Messages.UNIQUE_CONSTRAINT_PATTERN, Messages.UNIQUE_CONSTRAINT_MESSAGE);
							break;
						case Npgsql.PostgresErrorCodes.NumericValueOutOfRange: break;
						case Npgsql.PostgresErrorCodes.StringDataRightTruncation: break;
					};
				}

				if (!String.IsNullOrEmpty(message))
				{
					throw new Nucleus.Abstractions.DataProviderException(message, exception);
				}
			}
		}
	}

	internal class Messages
	{
		internal const string UNIQUE_CONSTRAINT_PATTERN = @"SQLite Error 19: 'UNIQUE constraint failed: (?<columns>.*)'.";
		internal const string UNIQUE_CONSTRAINT_MESSAGE = @"The combination of {columns} must be unique.";

		internal const string NOT_NULL_PATTERN = @"cannot insert the value NULL into column '(?<column>.*?)', table '.*\..*\.(?<table>.*?)'";
		internal const string NOT_NULL_MESSAGE = "The '{column}' field is required.";

		internal const string FOREIGN_KEY_PATTERN = "the delete statement conflicted .*constraint \"(?<constraint>.*?)\"";
		internal const string FOREIGN_KEY_MESSAGE = "{constraint}";
	}
	
}
