using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Layout;

namespace Nucleus.Core.Layout
{
	public static class LayoutExtensions
	{
		/// <summary>
		/// Add layout classes to the dependency injection container
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static IServiceCollection AddNucleusLayout(this IServiceCollection services)
		{
			// Layout components require a scoped context object
			services.AddScoped<Context>();

			// Container processing requires a scoped ContainerContext object
			services.AddScoped<ContainerContext>();

			services.AddScoped<PageRoutingMiddleware>();
			services.AddScoped<ModuleRoutingMiddleware>();
			services.AddScoped<IModuleContentRenderer, ModuleContentRenderer>();

			return services;
		}

	}
}
