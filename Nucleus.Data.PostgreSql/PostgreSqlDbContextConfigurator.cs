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

		/// <summary>
		/// Parse a database exception and match it to a friendly error message.  If a match is found, throw a <see cref="DataProviderException"/> to wrap the
		/// original exception with a more friendly error message.
		/// </summary>
		/// <param name="exception"></param>
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
		internal const string UNIQUE_CONSTRAINT_PATTERN = "duplicate key value violates unique constraint \"(?<constraint_name>.*?)\"";
		// Message is empty so that if the Nucleus.Data.EntityFramework.DbContextConfigurator.ConstraintMessage method
		// does not have a value for the constraint name, the original exception is used.
		internal const string UNIQUE_CONSTRAINT_MESSAGE = "";

		internal const string NOT_NULL_PATTERN = "null value in column \"(?<column>.*?)\" of relation \"(?<table>.*?)\" violates not-null constraint";
		internal const string NOT_NULL_MESSAGE = "The '{column}' field is required.";

		internal const string FOREIGN_KEY_PATTERN = "insert or update on table \"(?<table>.*?)\" violates foreign key constraint \"(?<constraint_name>.*?)\"";
		// Message is empty so that if the Nucleus.Data.EntityFramework.DbContextConfigurator.ConstraintMessage method
		// does not have a value for the constraint name, the original exception is used.
		internal const string FOREIGN_KEY_MESSAGE = "";
	}
	
}
