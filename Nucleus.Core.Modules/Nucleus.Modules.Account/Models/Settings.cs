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

      public const string PreventAutoLogin = "login:preventautologin";
    }

		public Boolean AllowRememberMe { get; set; }

		public Boolean AllowUsernameRecovery { get; set; }

		public Boolean AllowPasswordReset { get; set; }

    public Boolean PreventAutoLogin { get; set; }

    public void ReadSettings(PageModule module)
		{
      this.AllowPasswordReset = module.ModuleSettings.Get(ModuleSettingsKeys.AllowPasswordReset, true);
      this.AllowUsernameRecovery = module.ModuleSettings.Get(ModuleSettingsKeys.AllowUsernameRecovery, true);
      this.AllowRememberMe = module.ModuleSettings.Get(ModuleSettingsKeys.AllowRememberMe, true);

      this.PreventAutoLogin = module.ModuleSettings.Get(ModuleSettingsKeys.AllowRememberMe, true);
    }

		public void WriteSettings(PageModule module)
		{
			module.ModuleSettings.Set(ModuleSettingsKeys.AllowPasswordReset, this.AllowPasswordReset);
			module.ModuleSettings.Set(ModuleSettingsKeys.AllowUsernameRecovery, this.AllowUsernameRecovery);
			module.ModuleSettings.Set(ModuleSettingsKeys.AllowRememberMe, this.AllowRememberMe);
      module.ModuleSettings.Set(ModuleSettingsKeys.PreventAutoLogin, this.PreventAutoLogin);
    }
	}
}
