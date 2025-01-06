using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Account.MfaProviders.Totp;

public class TotpProvider : IMfaProvider
{

  public string GetChallengeView()
  {
    return "~/MfaProviders/Totp/VerifyOtp.cshtml";
  }

  public string GetSettingsView()
  {
    return "~/MfaProviders/Totp/OtpSettings.cshtml";
  }

  IMfaProviderOptions IMfaProvider.GetSettings(User loginUser)
  {
    return GetSettings() as IMfaProviderOptions;
  }

  private Options GetSettings()
  {
    throw new NotImplementedException();
  }


  public void SetSettings(User loginUser, IMfaProviderOptions settings)
  {
    throw new NotImplementedException();
  }

  private void SetSettings(Options settings)
  {
    throw new NotImplementedException();
  }

  public bool Verify(User loginUser, List<Claim> claims)
  {
    throw new NotImplementedException();
  }
  public bool Challenge(User loginUser)
  {
    return true;
  }

}
