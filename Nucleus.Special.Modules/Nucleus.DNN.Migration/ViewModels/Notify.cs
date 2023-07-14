using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.DNN.Migration.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.ViewModels;

public class Notify
{
  public Boolean IsMailConfigured { get; set; }
  public List<Models.NotifyUser> Users { get; set; }

  public IEnumerable<Nucleus.Abstractions.Models.Mail.MailTemplate> MailTemplates { get; set; }
  public Guid NotifyUserTemplateId { get; set; }

}
