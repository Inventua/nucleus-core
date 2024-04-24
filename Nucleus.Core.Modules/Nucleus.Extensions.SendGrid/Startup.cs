using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Mail;
using Microsoft.Extensions.Configuration;

[assembly: HostingStartup(typeof(Nucleus.Extensions.SendGrid.Startup))]

namespace Nucleus.Extensions.SendGrid;

public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {    
    builder.ConfigureServices((context, services) =>
    {      
      services.AddTransient<IMailClient, SendGridMailClient>();
      services.Configure<SendGridMailOptions>(context.Configuration.GetSection(SendGridMailOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);      
    });
  }
}

