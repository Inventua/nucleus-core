using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Account.Models
{
	public class Settings
	{
		internal class ModuleSettingsKeys
		{
			public const string AllowRememberMe = "login:allowrememberme";
			public const string AllowUsernameRecovery = "login:allowusernamerecovery";
			public const string AllowPasswordReset = "login:allowpasswordreset";
    }

    public Boolean AllowRememberMe { get; set; } = true;

		public Boolean AllowUsernameRecovery { get; set; } = true;

    public Boolean AllowPasswordReset { get; set; } = true;

    public void ReadSettings(PageModule module)
		{
      this.AllowPasswordReset = module.ModuleSettings.Get(ModuleSettingsKeys.AllowPasswordReset, this.AllowPasswordReset);
      this.AllowUsernameRecovery = module.ModuleSettings.Get(ModuleSettingsKeys.AllowUsernameRecovery, this.AllowUsernameRecovery);
      this.AllowRememberMe = module.ModuleSettings.Get(ModuleSettingsKeys.AllowRememberMe, this.AllowRememberMe);
    }

		public void WriteSettings(PageModule module)
		{
			module.ModuleSettings.Set(ModuleSettingsKeys.AllowPasswordReset, this.AllowPasswordReset);
			module.ModuleSettings.Set(ModuleSettingsKeys.AllowUsernameRecovery, this.AllowUsernameRecovery);
			module.ModuleSettings.Set(ModuleSettingsKeys.AllowRememberMe, this.AllowRememberMe);
    }
  }
}
