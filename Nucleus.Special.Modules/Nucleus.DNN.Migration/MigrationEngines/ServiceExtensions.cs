using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Nucleus.DNN.Migration.MigrationEngines;

public static class ServiceExtensions
{  
  public static void AddMigrationEngine<TModel, TEngine>(this IServiceCollection services) 
    where TModel : class 
    where TEngine : class, IMigrationEngine<TModel>
  {
    services.AddSingleton<IMigrationEngine<TModel>, TEngine>();    
  }

  public static IMigrationEngine<TModel> CreateEngine<TModel>(this IServiceProvider services) where TModel : class
  {
    return services.GetService<IMigrationEngine<TModel>>();
  }
}
