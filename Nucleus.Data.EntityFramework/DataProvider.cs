using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System.Linq;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// Base class used by entity-framework data providers.
	/// </summary>
	/// <remarks>
	/// Nucleus core and module data provider which use entity framework classes inherit this class, which contains an implementation of
	/// the schema migration functions.  Data provider implementations which register a related DataProviderMigration class must inherit 
	/// this class.
	/// </remarks>
	public abstract class DataProvider : Nucleus.Data.Common.DataProvider, IDisposable
	{
		/// <summary>
		/// Entity framework DbContext.
		/// </summary>
		public Nucleus.Data.EntityFramework.DbContext Context { get; }

		/// <summary>
		/// Logger used to log messages.
		/// </summary>
		protected ILogger<DataProvider> Logger { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="logger"></param>
		public DataProvider(DbContext context, ILogger<DataProvider> logger)
		{
			this.Context = context;
			this.Logger = logger;
		}

		/// <summary>
		/// Attempts a connection to the database.  Throws a database provider specific exception if not successful.
		/// </summary>
		public override void CheckConnection()
		{
			try
			{
				this.Context.Database.OpenConnection();
			}
			catch(Exception e)
			{
				Nucleus.Data.Common.ConnectionException wrapped = new(this.Context.Database.GetDbConnection(), this.Context.DbContextConfigurator.DatabaseConnectionOption, e);
				Logger?.LogError(wrapped, "");
				throw wrapped;
			}
		}

		/// <summary>
		/// Get the key for the database which corresponds to a section in the database configuration settings file.
		/// </summary>
		/// <returns></returns>
		public override string GetDatabaseKey()
		{			
			Nucleus.Data.EntityFramework.DbContext context = this.Context as Nucleus.Data.EntityFramework.DbContext;

			if (context != null && context.DbContextConfigurator != null && context.DbContextConfigurator.DatabaseConnectionOption != null)
			{
				return (this.Context as Nucleus.Data.EntityFramework.DbContext).DbContextConfigurator.DatabaseConnectionOption?.Key;
			}

			return "";

		}
	}

}
