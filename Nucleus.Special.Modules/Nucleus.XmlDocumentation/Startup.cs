using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Search;

[assembly: HostingStartup(typeof(Nucleus.XmlDocumentation.Startup))]

namespace Nucleus.XmlDocumentation
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
				// search content producer
				services.AddTransient<IContentMetaDataProducer, ApiMetaDataProducer>();

			});
		}
	}
}
