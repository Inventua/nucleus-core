using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Core.DataProviders.Abstractions;

namespace Nucleus.Core.DataProviders.SQLite
{
	public static class SQLiteDataProviderExtensions
	{
		/// <summary>
		/// Add data provider and related/required objects to the dependency injection services collection
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static IServiceCollection AddSQLiteDataProvider(this IServiceCollection services)
		{
			services.ConfigureOptions<ConfigureSqlLiteDataProviderOptions>();

			services.AddSingleton< Nucleus.Core.DataProviders.Abstractions.SQLiteDataProviderOptions, SQLiteDataProviderOptions >();
			services.AddSingleton<DataProviderFactory>();

			services.AddTransient<ILayoutDataProvider, SQLite.SQLiteDataProvider>();
			services.AddTransient<IMailDataProvider, SQLite.SQLiteDataProvider>();
			services.AddTransient<IPermissionsDataProvider, SQLite.SQLiteDataProvider>();
			services.AddTransient<ISessionDataProvider, SQLite.SQLiteDataProvider>();
			services.AddTransient<IUserDataProvider, SQLite.SQLiteDataProvider>();
			services.AddTransient<IScheduledTaskDataProvider, SQLite.SQLiteDataProvider>();
			services.AddTransient<IFileSystemDataProvider, SQLite.SQLiteDataProvider>();
			services.AddTransient<IListDataProvider, SQLite.SQLiteDataProvider>();


			return services;
		}

		/// <summary>
		/// Configure the session data provider options object. 
		/// </summary>
		private class ConfigureSqlLiteDataProviderOptions : IPostConfigureOptions<SQLiteDataProviderOptions>
		{
			private IConfiguration Configuration { get; }
			/// <summary>
			/// Constructor
			/// </summary>
			public ConfigureSqlLiteDataProviderOptions(IConfiguration configuration)
			{
				this.Configuration = configuration;
			}

			/// <summary>
			/// Perform configuration
			/// </summary>
			/// <param name="name"></param>
			/// <param name="options"></param>
			public void PostConfigure(string name, SQLiteDataProviderOptions options)
			{
				this.Configuration.Bind(SQLiteDataProviderOptions.Section, options);

			}
		}
	}
}

