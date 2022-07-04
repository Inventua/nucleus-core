using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.Common;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Data provider factory base class.  Provides common functionality for data providers.
	/// </summary>
	public class DataProviderFactory : IDataProviderFactory
	{
		private ILogger<DataProviderFactory> Logger { get; }
		private IServiceProvider RequestServices { get; }
		private Dictionary<string, Boolean> IsSchemaChecked { get; } = new();
		private Dictionary<System.Type, Boolean> IsConnectionChecked { get; } = new();

		private static readonly object lockObject = new();

		/// <summary>
		/// Constructor (called by DI)
		/// </summary>
		public DataProviderFactory(IServiceProvider requestServices, ILogger<DataProviderFactory> logger)
		{
			this.RequestServices = requestServices;
			this.Logger = logger;
		}

		/// <summary>
		/// Create a new instance of the specified data provider interface
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <returns>A data provider instance of the specified type.</returns>
		public TDataProvider CreateProvider<TDataProvider>()
		{
			TDataProvider provider = (TDataProvider)this.RequestServices.GetService<TDataProvider>();

			if (provider != null)
			{
				Boolean checkConnectionRequired = (!this.IsConnectionChecked.ContainsKey(provider.GetType()) || !this.IsConnectionChecked[provider.GetType()]);

				if (checkConnectionRequired)
				{
					lock (lockObject)
					{
						if (!this.IsConnectionChecked.ContainsKey(provider.GetType()) || !this.IsConnectionChecked[provider.GetType()])
						{
							DataProvider baseProvider = provider as DataProvider;

							if (baseProvider != null)
							{
								baseProvider.CheckConnection();
							}

							this.IsConnectionChecked.Add(provider.GetType(), true);
						}
					}
				}

				DataProviderMigration migration = (DataProviderMigration)this.RequestServices.GetService(typeof(DataProviderMigration<>).MakeGenericType(new Type[] { provider.GetType() }));

				// There doesn't have to be a DataProviderConfiguration<T> in the services collection, but if there is, we can
				// check for new schema updates and perform schema migration.
				if (migration != null)
				{
					Boolean checkSchemaRequired = (!this.IsSchemaChecked.ContainsKey(migration.SchemaName) || !this.IsSchemaChecked[migration.SchemaName]);

					if (checkSchemaRequired)
					{
						lock (lockObject)
						{
							if (!this.IsSchemaChecked.ContainsKey(migration.SchemaName) || !this.IsSchemaChecked[migration.SchemaName])
							{
								Logger.LogInformation("Checking database schema [{name}].", migration.SchemaName);
								migration.CheckDatabaseSchema();

								if (this.IsSchemaChecked.ContainsKey(migration.SchemaName))
								{
									this.IsSchemaChecked[migration.SchemaName] = true;
								}
								else
								{
									this.IsSchemaChecked.Add(migration.SchemaName, true);
								}
							}
						}
					}
				}
			}


			return provider;
		}
	}
}
