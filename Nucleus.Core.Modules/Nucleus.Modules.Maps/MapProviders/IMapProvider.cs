using Nucleus.Modules.Maps.MapRenderers;
using Nucleus.Modules.Maps.MapGeocoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Maps.MapProviders;

internal interface IMapProvider
{
  public abstract Models.Settings GetSettings();

  public abstract IMapRenderer GetRenderer();

  public abstract IMapGeocoder GetGeocoder();
}