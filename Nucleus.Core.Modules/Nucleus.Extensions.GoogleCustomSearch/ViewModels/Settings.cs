using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.GoogleCustomSearch.ViewModels;

public class Settings : Models.Settings
{
  public const string DUMMY_API_KEY = "!@#NOT_CHANGED^&*";

  public string ApiKey { get; set; }

  public List<string> SafeSearchOptions { get; set; } = new();

}
