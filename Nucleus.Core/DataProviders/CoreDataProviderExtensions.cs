using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	public static class CoreDataProviderExtensions
	{
		/// <summary>
		/// Add data provider and related/required objects to the dependency injection services collection
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static IServiceCollection AddCoreDataProvider(this IServiceCollection services, IConfiguration configuration)
		{
			// The CoreDataProvider is an entity-framework data provider, so it needs a DbContext
			services.AddDbContext<CoreDataProviderDbContext>(ServiceLifetime.Transient);

			// Add core entities data provider
			services.AddTransient<CoreDataProvider>();

			// Add configured data providers
			services.AddDataProvider<CoreDataProvider>(configuration);

			// Add data provider interfaces.  These use the "factory" overload to refer to the CoreDataProvider which has been
			// injected into the dependency injection container above.
			services.AddTransient<ILayoutDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<IMailDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<IPermissionsDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<ISessionDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<IUserDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<IScheduledTaskDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<IFileSystemDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<IListDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<IContentDataProvider>(CoreEntityFrameworkDataProviderFactory);
			services.AddTransient<IApiKeyDataProvider>(CoreEntityFrameworkDataProviderFactory);
      services.AddTransient<IOrganizationDataProvider>(CoreEntityFrameworkDataProviderFactory);
      services.AddTransient<IExtensionsStoreDataProvider>(CoreEntityFrameworkDataProviderFactory);

      return services;
		}

		private static CoreDataProvider CoreEntityFrameworkDataProviderFactory(System.IServiceProvider serviceProvider)
		{
			return serviceProvider.GetRequiredService<CoreDataProvider>();
		}

	}
}

