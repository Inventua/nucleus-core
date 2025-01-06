using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Maps.MapRenderers;

public interface IMapRenderer
{
  public Task<System.IO.Stream> RenderMap(Settings settings);
}
