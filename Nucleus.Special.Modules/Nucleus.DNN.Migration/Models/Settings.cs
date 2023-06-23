using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models;
public class Settings
{
  public const string MODULESETTING_TITLE = "DNNMigration:title";

  public string Title { get; set; }

  public void ReadSettings(PageModule module)
  {
    //this.Title = module.ModuleSettings.Get(MODULESETTING_TITLE, "");
  }
}
