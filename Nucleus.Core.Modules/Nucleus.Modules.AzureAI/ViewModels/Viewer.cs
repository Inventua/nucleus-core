using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI.Chat;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Modules.AzureAI.ViewModels;
public class Viewer : Models.Settings
{
  public string Question { get; set; }

  public List<Models.ChatHistory> History { get; set; } = [];

}
