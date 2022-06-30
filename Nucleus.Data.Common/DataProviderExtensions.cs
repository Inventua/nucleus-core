using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Common data provider registration extensions.
	/// </summary>
	public static class DataProviderExtensions
	{
		/// <summary>
		/// Add data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses SqlServer.  This overload uses the default schema name for the data provider type.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		/// <remarks>
		/// The logic to check whether a specific database provider is the right one to use for the specified <typeparamref name="TDataProvider"/> 
		/// is implemented within each <see cref="IDatabaseProvider"/> implementation.
		/// Data providers which are based on Nucleus.Data.EntityFramework do not need to call this directly - it is called by the
		/// Nucleus.Data.EntityFramework.DataProviderExtensions.AddDataProvider() methods.
		/// </remarks>
		static public IServiceCollection AddDataProvider<TDataProvider>(this IServiceCollection services, IConfiguration configuration)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			string schemaName = typeof(TDataProvider).GetDefaultSchemaName();
			return AddDataProvider<TDataProvider>(services, configuration, schemaName);
		}

		/// <summary>
		/// Add data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses SqlServer.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		/// <remarks>
		/// The logic to check whether a specific database provider is the right one to use for the specified <typeparamref name="TDataProvider"/> 
		/// is implemented within each <see cref="IDatabaseProvider"/> implementation.
		/// Data providers which are based on Nucleus.Data.EntityFramework do not need to call this directly - it is called by the
		/// Nucleus.Data.EntityFramework.DataProviderExtensions.AddDataProvider() methods.
		/// </remarks>
		static public IServiceCollection AddDataProvider<TDataProvider>(this IServiceCollection services, IConfiguration configuration, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider
		{
			Boolean success = false;

			// Get database options
			DatabaseOptions options = new();
			configuration.GetSection(DatabaseOptions.Section).Bind(options, options => options.BindNonPublicProperties = true);

			// Find IConfigureDataProvider implementations		
			List<Type> dataProviderConfigImplementations = System.Runtime.Loader.AssemblyLoadContext.All
				.SelectMany(context => context.Assemblies)
				.SelectMany(assm => GetTypes(assm).Where(type => typeof(IDatabaseProvider).IsAssignableFrom(type) && !type.Equals(typeof(IDatabaseProvider))))
				.ToList();

			// Add data provider objects for the data provider specified by <T>.  The database provider required for <T> is specified in
			// config. 
			foreach (Type implementation in dataProviderConfigImplementations)
			{
				IDatabaseProvider instance = Activator.CreateInstance(implementation) as IDatabaseProvider;
				if (instance != null)
				{
					// Get connection for the specified schema name.  If it is found, add Sqlite data provider objects to the services collection.
					DatabaseConnectionOption connectionOption = options.GetDatabaseConnection(schemaName);

					if (connectionOption != null && connectionOption.Type.Equals(instance.TypeKey(), StringComparison.OrdinalIgnoreCase))
					{
						instance.AddDataProvider<TDataProvider>(services, connectionOption, schemaName);						
						success = true;
						break;						
					}
				}
			}

			if (!success)
			{
				throw new InvalidOperationException($"Nucleus could not find a configured data provider for {typeof(TDataProvider).FullName}.");
			}

			return services;
		}

		/// <summary>
		/// Get database diagnostics information for the specified schema.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		static public Dictionary<string, string> GetDataProviderInformation(IServiceProvider services, string schemaName)
		{
			Dictionary<string, string> results = new();

			// Get database options
			DatabaseOptions options = new();
			IConfiguration configuration = services.GetService<IConfiguration>();
			configuration.GetSection(DatabaseOptions.Section).Bind(options, options => options.BindNonPublicProperties = true);

			// Find IConfigureDataProvider implementations		
			List<Type> dataProviderConfigImplementations = System.Runtime.Loader.AssemblyLoadContext.All
				.SelectMany(context => context.Assemblies)
				.SelectMany(assm => GetTypes(assm))// assm => assm.GetTypes())
				.Where(type => typeof(IDatabaseProvider).IsAssignableFrom(type) && !type.Equals(typeof(IDatabaseProvider)))
				.ToList();

			// Determine which database provider services the specified schema name and retrieve database diagnostic information
			foreach (Type implementation in dataProviderConfigImplementations)
			{
				IDatabaseProvider instance = Activator.CreateInstance(implementation) as IDatabaseProvider;
				if (instance != null)
				{
					// Get connection for the specified schema name.  If it is found, get database information.
					DatabaseConnectionOption connectionOption = options.GetDatabaseConnection(schemaName);

					if (connectionOption != null && connectionOption.Type.Equals(instance.TypeKey(), StringComparison.OrdinalIgnoreCase))
					{
						foreach (KeyValuePair<string, string> value in instance.GetDatabaseInformation(services, connectionOption, schemaName))
						{
							results.Add(value.Key, value.Value);
						}

						break;
					}					
				}
			}

			return results;
		}

		private static Type[] GetTypes(System.Reflection.Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (System.Reflection.ReflectionTypeLoadException)
			{
				return new Type[] { };
			}
		}

		/// <summary>
		/// Add the default data provider factory to the services collection.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static IServiceCollection AddDataProviderFactory(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);
			services.AddSingleton<IDataProviderFactory, DataProviderFactory>();

			return services;
		}

		/// <summary>
		/// Return the default namespace for a type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <remarks>
		/// The specified type would typically be a data provider.  The default schema name is derived by returning the part of the namespace 
		/// before ".DataProvider".  If the namespace does not contain ".DataProvider", this function returns an empty string.  The convention
		/// for modules is to us a namespace with ".DataProviders" (plural), but using ".DataProvider" (singular) as the search value handles
		/// both plural and singular cases.
		/// </remarks>
		public static string GetDefaultSchemaName(this System.Type type)
		{
			string typeNamespace = type.Namespace;

			// set the default schema name to the namespace part before ".DataProvider" if the namespace container ".DataProvider"
			if (typeNamespace.Contains(".DataProvider", StringComparison.OrdinalIgnoreCase))
			{
				return typeNamespace.Substring(0, typeNamespace.LastIndexOf(".DataProvider", StringComparison.OrdinalIgnoreCase));
			}

			return "";
		}
	}
}
