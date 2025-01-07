using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Account.MfaProviders;

public interface IMfaProvider
{
  public string GetSettingsView();

  public string GetChallengeView();

  public abstract IMfaProviderOptions GetSettings(User loginUser);

  public void SetSettings(User loginUser, IMfaProviderOptions settings);

  public bool Challenge(User loginUser);

  public Boolean Verify(User loginUser, List<System.Security.Claims.Claim> claims);


}
