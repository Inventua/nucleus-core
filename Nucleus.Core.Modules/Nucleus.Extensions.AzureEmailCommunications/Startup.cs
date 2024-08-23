using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Mail;

[assembly: HostingStartup(typeof(Nucleus.Extensions.AzureEmailCommunications.Startup))]

namespace Nucleus.Extensions.AzureEmailCommunications;

public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddTransient<IMailClient, AzureEmailCommunicationsClient>();
      services.Configure<AzureEmailCommunicationsOptions>(context.Configuration.GetSection(AzureEmailCommunicationsOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);
    });
  }
}
