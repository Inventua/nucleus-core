using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Mail;
using Microsoft.Extensions.Configuration;
using DocumentFormat.OpenXml.Spreadsheet;

[assembly: HostingStartup(typeof(Nucleus.Extensions.Smtp.Startup))]

namespace Nucleus.Extensions.Smtp;

public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {    
    builder.ConfigureServices((context, services) =>
    {      
      services.AddTransient<IMailClient, SmtpMailClient>();
      services.Configure<SmtpMailOptions>(context.Configuration.GetSection(SmtpMailOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);

      services.AddTransient<IMailClient, PickupDirectoryMailClient>();
      services.Configure<PickupDirectoryMailOptions>(context.Configuration.GetSection(PickupDirectoryMailOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);

    });
  }
}

