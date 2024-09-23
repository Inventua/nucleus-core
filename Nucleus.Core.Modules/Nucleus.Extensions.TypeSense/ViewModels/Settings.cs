using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Extensions.TypeSense.ViewModels;

public class Settings : Models.Settings
{
  public const string DUMMY_APIKEY = "!@#NOT_CHANGED^&*";

  // This constructor is used by model binding
  public Settings() { }

  public Settings(Site site) : base(site)
  {
    if (String.IsNullOrEmpty(base.EncryptedApiKey))
    {
      this.ApiKey = "";
    }
  }

  public string ApiKey { get; set; } = DUMMY_APIKEY;
}
