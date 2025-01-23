using Nucleus.Abstractions.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Account.ViewModels;

public class VerifyOtp //: Models.Settings
{
  public string Controller { get; set; }

  public string Message { get; set; }

  public string ReturnUrl { get; set; }

  public Guid SessionId { get; set; }

  public string OneTimePassword { get; set; }

  public Boolean Is2FARegistered { get; set; }

  public string QrCodeAsSvg { get; set; }

  public string IsSetup { get; set; }
}
