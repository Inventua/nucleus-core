using System;
using System.Collections.Generic;

namespace Nucleus.Modules.IFrame.ViewModels;
public class Viewer : Models.Settings
{
  public Dictionary<string, object> FrameAttributes { get; set; } = new();
}
