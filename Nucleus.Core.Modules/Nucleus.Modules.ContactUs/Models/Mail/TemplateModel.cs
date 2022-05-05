using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.ContactUs.Models.Mail
{
  public class TemplateModel
  {
    public Site Site { get; set; }
    public Models.Message Message { get; set; }

  }
}
