using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.CopilotClient.ViewModels;

public class Viewer : Models.Settings
{
  public Boolean IsConfigured { get; set; }
  public string ConversationId { get; set; }
  public string Question { get; set; }
  public string Watermark { get; set; }

  public Guid ModuleId { get; set; }

  public class Message
  {
    public string Watermark { get; set; }

    public IEnumerable<Response> Responses { get; set; }
    public IEnumerable<Citation> Citations { get; set; }
  }

  public class Citation
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }
  }

  public class Response
  {
    public string Text { get; set; }
    public string Type { get; set; }
  }
}