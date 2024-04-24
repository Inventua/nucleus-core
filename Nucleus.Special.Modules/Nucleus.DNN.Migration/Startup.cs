using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.EntityFramework;
using Nucleus.DNN.Migration.DataProviders;
using Nucleus.DNN.Migration.MigrationEngines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

[assembly: HostingStartup(typeof(Nucleus.DNN.Migration.Startup))]

namespace Nucleus.DNN.Migration;

public class Startup : IHostingStartup
{
  public const string DNN_SCHEMA_NAME = "DNN";

  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddDataProvider<DNNMigrationDataProvider, DNNMigrationDataProvider, DNNMigrationDbContext>(context.Configuration);
      try
      {
        services.AddDataProvider<DNNDataProvider, DNNDataProvider, DNNDbContext>(context.Configuration, DNN_SCHEMA_NAME);
      }
      catch(InvalidOperationException) 
      {
        // suppress.  This happens when there is no database configuration schema with name="DNN".  The GetDNNVersion function return NULL
        // when there is no DNNDataProvider registered, and the main view shows a warning message.
      }
      
      services.AddSingleton<DNNMigrationManager>();
      
      services.AddMigrationEngine<Models.DNN.RoleGroup, MigrationEngines.RoleGroupMigration>();
      services.AddMigrationEngine<Models.DNN.Role, MigrationEngines.RoleMigration>();
      services.AddMigrationEngine<Models.DNN.List, MigrationEngines.ListMigration>();
      services.AddMigrationEngine<Models.DNN.Page, MigrationEngines.PageMigration>();
      services.AddMigrationEngine<Models.DNN.User, MigrationEngines.UserMigration>();
      services.AddMigrationEngine<Models.DNN.Modules.NTForums.Forum, MigrationEngines.NTForumsMigration>();
      services.AddMigrationEngine<Models.DNN.Modules.ActiveForums.Forum, MigrationEngines.ActiveForumsMigration>();
      services.AddMigrationEngine<Models.NotifyUser, MigrationEngines.NotifyUsers>();
      services.AddMigrationEngine<Models.DNN.Folder, MigrationEngines.FilesMigration>();

      // Add module content migration classes to DI
      foreach (Type type in GetModuleContentMigrationImplementations())
      {
        services.AddSingleton(typeof(MigrationEngines.ModuleContent.ModuleContentMigrationBase), type);
      }
    });
  }

  /// <summary>
  /// Find MigrationEngines.ModuleContent.ModuleContentMigrationBase implementations.
  /// </summary>
  /// <returns></returns>
  static IEnumerable<Type> GetModuleContentMigrationImplementations()
  {
    return System.Runtime.Loader.AssemblyLoadContext.All
      .SelectMany(context => context.Assemblies)
      .SelectMany(assm => GetTypes(assm)
        .Where(type => typeof(MigrationEngines.ModuleContent.ModuleContentMigrationBase).IsAssignableFrom(type) && !type.Equals(typeof(MigrationEngines.ModuleContent.ModuleContentMigrationBase))));
  }

  private static Type[] GetTypes(System.Reflection.Assembly assembly)
  {
    try
    {
      return assembly.GetExportedTypes();
    }
    catch (Exception)
    {
      return Array.Empty<Type>();
    }
  }
}

