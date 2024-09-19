using System.Collections.Generic;

namespace Nucleus.Extensions.BingCustomSearch.ViewModels;

public class Settings : Models.Settings
{
  public const string DUMMY_API_KEY = "!@#NOT_CHANGED^&*";

  public string ApiKey { get; set; }

}
