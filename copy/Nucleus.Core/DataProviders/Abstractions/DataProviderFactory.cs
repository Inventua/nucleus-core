using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Core.DataProviders.Abstractions
{
	/// <summary>
	/// Data provider factory base class.  Provides common functionality for data providers.
	/// </summary>
	public class DataProviderFactory
	{
		private IHttpContextAccessor HttpContextAccessor { get; }
		private ILogger<DataProvider> Logger { get; }
		private IServiceProvider RequestServices { get; }

		private Dictionary<string, Boolean> IsSchemaChecked { get; } = new();

		/// <summary>
		/// Constructor (called by DI)
		/// </summary>
		public DataProviderFactory(IServiceProvider requestServices, IHttpContextAccessor httpContextAccessor, ILogger<DataProvider> logger)
		{
			this.RequestServices = requestServices;
			this.HttpContextAccessor = httpContextAccessor;
			this.Logger = logger;
		}

		/// <summary>
		/// Create a new instance of SessionDataProvider
		/// </summary>
		/// <returns>SessionDataProvider</returns>
		public T CreateProvider<T>() where T : class, Abstractions.IDataProvider
		{
			Abstractions.IDataProvider provider = (Abstractions.IDataProvider)this.RequestServices.GetService<T>();			

			if (provider == null)
			{
				throw new ArgumentException($"Unable to get a service of type {typeof(T)} from the services collection.", nameof(T));
			}

			if (!this.IsSchemaChecked.ContainsKey(provider.SchemaName) || !this.IsSchemaChecked[provider.SchemaName])
			{
				Logger.LogInformation("Checking database schema.");
				provider.CheckDatabaseSchema(provider.SchemaName);

				if (this.IsSchemaChecked.ContainsKey(provider.SchemaName))
				{
					this.IsSchemaChecked[provider.SchemaName] = true;
				}
				else
				{
					this.IsSchemaChecked.Add(provider.SchemaName, true);
				}				
			}

			return provider as T;
		}
	}
}
