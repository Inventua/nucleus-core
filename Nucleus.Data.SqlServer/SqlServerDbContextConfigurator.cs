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
				options.UseSqlServer(this.DatabaseConnectionOption.ConnectionString);
				return true;
			}

			return false;
		}

		/// <inheritdoc/>
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
						message = ParseException(dbException, Messages.FOREIGN_KEY_PATTERN, Messages.FOREIGN_KEY_MESSAGE);
						break;
					case CannotInsertNull:
						message = ParseException(dbException, Messages.NOT_NULL_PATTERN, Messages.NOT_NULL_MESSAGE);
						break;
					case CannotInsertDuplicateKeyUniqueIndex:
					case CannotInsertDuplicateKeyUniqueConstraint:
						message = ParseException(dbException, Messages.UNIQUE_CONSTRAINT_PATTERN, Messages.UNIQUE_CONSTRAINT_MESSAGE);
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

	internal class Messages
	{
		internal const string UNIQUE_CONSTRAINT_PATTERN = @"Cannot insert duplicate key row in object 'dbo\.(?<table>.*)' with unique index '(?<constraint_name>.*)'\. The duplicate key value is .*\.";
		// Message is empty so that if the Nucleus.Data.EntityFramework.DbContextConfigurator.ConstraintMessage method
		// does not have a value for the index name, the original exception is used, because Sql server messages do not
		// contain the values which caused the error.
		internal const string UNIQUE_CONSTRAINT_MESSAGE = @"";

		internal const string NOT_NULL_PATTERN = @"cannot insert the value NULL into column '(?<column>.*?)', table '.*\..*\.(?<table>.*?)'";
		internal const string NOT_NULL_MESSAGE = "The '{column}' field is required.";

		internal const string FOREIGN_KEY_PATTERN = "the delete statement conflicted .*constraint \"(?<constraint>.*?)\"";
		internal const string FOREIGN_KEY_MESSAGE = "{constraint_name}";
	}

}

