using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.Common;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// Extensions for entity-framework data provider dependency configuration.
	/// </summary>
	static public class DataProviderExtensions
	{		
		/// <summary>
		/// Add the specified data provider and DbContext to the services collection.
		/// </summary>
		/// <typeparam name="TDataProviderInterface">
		/// Interface used by modules to access the data provider.  This type is used as a key to the dependency injection service collection.
		/// </typeparam>
		/// <typeparam name="TDataProvider">
		/// Data provider class.  Must implement <typeparamref name="TDataProviderInterface"/> and <see cref="DataProvider"/>.
		/// </typeparam>
		/// <typeparam name="TDbContext">
		/// Entity framework DbContext class.
		/// </typeparam>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		static public IServiceCollection AddDataProvider<TDataProviderInterface, TDataProvider, TDbContext>(this IServiceCollection services, IConfiguration configuration)
			where TDataProviderInterface : class
			where TDataProvider : DataProvider, TDataProviderInterface
			where TDbContext : DbContext	
		{
			services.AddDataProvider<TDataProvider>(configuration);

			services.AddDbContext<TDbContext>(ServiceLifetime.Transient);

			// we add the *same instance* of the data provider with two interfaces - one for TDataProviderInterface, to be used by the module, and one
			// as "itself", so that any configured DataProviderMigration classes can get an instance if required.
			services.AddTransient<TDataProvider>();
			services.AddTransient<TDataProviderInterface>(serviceProvider => serviceProvider.GetRequiredService<TDataProvider>()); ;

			return services;
		}

	}
}
