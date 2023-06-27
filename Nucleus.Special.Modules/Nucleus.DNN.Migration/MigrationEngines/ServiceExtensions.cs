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
    where TModel : Models.DNN.DNNEntity 
    where TEngine : MigrationEngineBase<TModel>
  {
    services.AddScoped<MigrationEngineBase<TModel>, TEngine>();    
  }

  public static MigrationEngineBase<TModel> CreateEngine<TModel>(this IServiceProvider services) 
    where TModel : Models.DNN.DNNEntity
  {
    return services.GetService<MigrationEngineBase<TModel>>();
  }
}
