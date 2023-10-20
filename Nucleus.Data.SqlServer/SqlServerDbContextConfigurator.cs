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
		public SqlServerDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions) : base(databaseOptions) { }

		/// <summary>
		/// Configure the DbContextOptionsBuilder for Sql server
		/// </summary>
		/// <param name="options"></param>
		public override Boolean Configure(DbContextOptionsBuilder options)
		{
			if (this.DatabaseConnectionOption != null)
			{
				options.UseSqlServer(this.DatabaseConnectionOption.ConnectionString, options =>
        {
          options.EnableRetryOnFailure();
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
			const int ReferenceConstraint = 547;
			const int CannotInsertNull = 515;
			const int CannotInsertDuplicateKeyUniqueIndex = 2601;
			const int CannotInsertDuplicateKeyUniqueConstraint = 2627;
			const int ArithmeticOverflow = 8115;
			const int StringOrBinaryDataWouldBeTruncated = 8152;

			if (exception.InnerException is Microsoft.Data.SqlClient.SqlException)
			{
				string message = "";

				Microsoft.Data.SqlClient.SqlException dbException = exception.InnerException as Microsoft.Data.SqlClient.SqlException;

				switch (dbException.Number)
				{
					case ReferenceConstraint:
						message = ParseException(dbException, MessagePatterns.FOREIGN_KEY_PATTERN, MessagePatterns.FOREIGN_KEY_MESSAGE);
						break;
					case CannotInsertNull:
						message = ParseException(dbException, MessagePatterns.NOT_NULL_PATTERN, MessagePatterns.NOT_NULL_MESSAGE);
						break;
					case CannotInsertDuplicateKeyUniqueIndex:
					case CannotInsertDuplicateKeyUniqueConstraint:
						message = ParseException(dbException, MessagePatterns.UNIQUE_CONSTRAINT_PATTERN, MessagePatterns.UNIQUE_CONSTRAINT_MESSAGE);
						break;
					case ArithmeticOverflow: break;
					case StringOrBinaryDataWouldBeTruncated: break;
				};

				if (!String.IsNullOrEmpty(message))
				{
					throw new Nucleus.Abstractions.DataProviderException(message, exception);
				}
			}
		}
	}

	internal class MessagePatterns
	{
		internal const string UNIQUE_CONSTRAINT_PATTERN = @"duplicate key row in object '.*\.(?<table>.*)' with unique index '(?<constraint_name>.*)'";
		// Message is empty so that if the Nucleus.Data.EntityFramework.DbContextConfigurator.ConstraintMessage method
		// does not have a value for the index name, the original exception is used.
		internal const string UNIQUE_CONSTRAINT_MESSAGE = "";

		internal const string NOT_NULL_PATTERN = @"cannot insert the value NULL into column '(?<column>.*?)', table '.*\..*\.(?<table>.*?)'";
		internal const string NOT_NULL_MESSAGE = "The '{column}' field is required.";

		internal const string FOREIGN_KEY_PATTERN = "FOREIGN KEY constraint \"(?<constraint_name>.*?)\"";
		internal const string FOREIGN_KEY_MESSAGE = "";
	}

}

