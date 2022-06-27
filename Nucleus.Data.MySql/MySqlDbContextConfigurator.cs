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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		public MySqlDbContextConfigurator(IOptions<DatabaseOptions> databaseOptions) : base(databaseOptions) { }

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

		/// <inheritdoc/>
		public override void ParseException(DbUpdateException exception)
		{
			// This code is inspired by, and uses code from https://github.com/Giorgi/EntityFramework.Exceptions.  https://www.apache.org/licenses/LICENSE-2.0

			if (exception.InnerException is MySqlConnector.MySqlException)
			{
				string message = "";

				MySqlConnector.MySqlException? dbException = exception.InnerException as MySqlConnector.MySqlException;

				if (dbException != null)
				{
					switch (dbException.ErrorCode)
					{
						case MySqlConnector.MySqlErrorCode.ColumnCannotBeNull:
							message = ParseException(dbException, Messages.NOT_NULL_PATTERN, Messages.NOT_NULL_MESSAGE);
							break;
						case MySqlConnector.MySqlErrorCode.DuplicateKeyEntry:
							// unique constraint
							message = ParseException(dbException, Messages.UNIQUE_CONSTRAINT_PATTERN, Messages.UNIQUE_CONSTRAINT_MESSAGE);
							break;
						case MySqlConnector.MySqlErrorCode.WarningDataOutOfRange:
							// NumericOverflow:  This would be caused by a bug, so we don't parse this one.
							break;
						case MySqlConnector.MySqlErrorCode.DataTooLong:
							// DatabaseError.MaxLength:  This would be caused by a bug, so we don't parse this one.
							break;
						case MySqlConnector.MySqlErrorCode.NoReferencedRow:
						case MySqlConnector.MySqlErrorCode.RowIsReferenced:
						case MySqlConnector.MySqlErrorCode.NoReferencedRow2:
						case MySqlConnector.MySqlErrorCode.RowIsReferenced2:
							// Foreign key constraint
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
			internal const string UNIQUE_CONSTRAINT_PATTERN = "Duplicate entry '.*' for key '(?<constraint_name>.*?)'";
			// Message is empty so that if the Nucleus.Data.EntityFramework.DbContextConfigurator.ConstraintMessage method
			// does not have a value for the constraint name, the original exception is used.
			internal const string UNIQUE_CONSTRAINT_MESSAGE = "";

			internal const string NOT_NULL_PATTERN = @"Column '(?<column>.*?)' cannot be null";
			internal const string NOT_NULL_MESSAGE = "The '{column}' field is required.";

			internal const string FOREIGN_KEY_PATTERN = "a foreign key constraint fails .*CONSTRAINT `(?<constraint_name>.*?)`";
			// Message is empty so that if the Nucleus.Data.EntityFramework.DbContextConfigurator.ConstraintMessage method
			// does not have a value for the constraint name, the original exception is used.
			internal const string FOREIGN_KEY_MESSAGE = "";
		}


	}
}
