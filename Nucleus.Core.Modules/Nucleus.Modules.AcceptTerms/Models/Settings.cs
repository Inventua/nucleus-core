using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.AcceptTerms.Models
{
  public class Settings
  {
    private const string MODULESETTING_TITLE = "acceptterms:title";
    private const string MODULESETTING_AGREEMENT_BODY = "acceptterms:agreementbody";
    private const string MODULESETTING_ACCEPTTEXT = "acceptterms:accepttext";
    private const string MODULESETTING_CANCELTEXT = "acceptterms:canceltext";
    private const string MODULESETTING_EFFECTIVEDATE = "acceptterms:effectivedate";

    public string Title { get; set; }

    public Content AgreementBody { get; set; }

    public string AcceptText { get; set; }

    public string CancelText { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public void GetSettings(PageModule module, TimeZoneInfo userTimeZoneInfo)
    {
      this.Title = module.ModuleSettings.Get(MODULESETTING_TITLE, "");
      this.AcceptText = module.ModuleSettings.Get(MODULESETTING_ACCEPTTEXT, "Accept");
      this.CancelText = module.ModuleSettings.Get(MODULESETTING_CANCELTEXT, "Cancel");

      DateTime? effectiveDateUtc = module.ModuleSettings.Get(MODULESETTING_EFFECTIVEDATE, (DateTime?)null);
     
      if (effectiveDateUtc.HasValue)
      {
        this.EffectiveDate = System.TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(effectiveDateUtc.Value, DateTimeKind.Utc), userTimeZoneInfo);
      }
      else
      {
        this.EffectiveDate = null;
      }
    }

    public void SetSettings(PageModule module, TimeZoneInfo userTimeZoneInfo)
    {
      DateTime? effectiveDateUtc = (this.EffectiveDate.HasValue ? System.TimeZoneInfo.ConvertTimeToUtc(this.EffectiveDate.Value, userTimeZoneInfo) : (DateTime?)null);

      module.ModuleSettings.Set(Models.Settings.MODULESETTING_TITLE, this.Title);
      module.ModuleSettings.Set(Models.Settings.MODULESETTING_ACCEPTTEXT, this.AcceptText);
      module.ModuleSettings.Set(Models.Settings.MODULESETTING_CANCELTEXT, this.CancelText);
      module.ModuleSettings.Set(Models.Settings.MODULESETTING_EFFECTIVEDATE, effectiveDateUtc);
    }
  }
}
