using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Nucleus.Abstractions.Models;

namespace Nucleus.XmlDocumentation.ViewModels
{
  public class Viewer
  {
    public Page Page { get; set; }

    public List<Models.ApiDocument> Documents { get; set; }
    public Boolean DefaultOpen { get; set; }

    public Models.ApiDocument SelectedDocument { get; set; }
    public Models.ApiClass SelectedClass { get; set; }

    public string Message { get; set; }
    public HtmlString WelcomeMessage { get; set; }
  }
}
