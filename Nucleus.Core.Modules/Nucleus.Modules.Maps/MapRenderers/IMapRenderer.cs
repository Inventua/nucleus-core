using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Maps.MapRenderers;

internal interface IMapRenderer
{
  public Task<System.IO.Stream> RenderMap(Site site, IHttpClientFactory httpClientFactory, Models.Settings settings);
}