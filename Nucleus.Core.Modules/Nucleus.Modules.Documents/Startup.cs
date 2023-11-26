using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Documents.DataProviders;
using Nucleus.Abstractions.Search;
using Nucleus.Data.EntityFramework;

[assembly: HostingStartup(typeof(Nucleus.Modules.Documents.Startup))]

namespace Nucleus.Modules.Documents;

public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddSingleton<DocumentsManager>();
      services.AddTransient<IContentMetaDataProducer, DocumentsMetaDataProducer>();
      services.AddDataProvider<IDocumentsDataProvider, DataProviders.DocumentsDataProvider, DataProviders.DocumentsDbContext>(context.Configuration);
    });
  }
}
