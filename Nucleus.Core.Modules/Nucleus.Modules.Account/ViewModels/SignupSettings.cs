using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Account.ViewModels;

public class SignupSettings : Models.SignupSettings
{
  public const string DUMMY_PASSWORD = "!@#NOT_CHANGED^&*";

  public string RecaptchaSecretKey { get; set; }
}
