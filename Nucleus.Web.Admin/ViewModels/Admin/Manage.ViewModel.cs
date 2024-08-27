using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin;

public class Manage
{
  public List<ControlPanelExtensionDefinition> Extensions { get; set; }
  public ControlPanelExtensionDefinition ControlPanelExtension { get; set; }
}
