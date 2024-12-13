using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Maps.MapGeocoders;

internal interface IMapGeocoder
{
  public Task<Models.GeocodingLocation> LookupAddress(Site site, IHttpClientFactory httpClientFactory, Models.Settings settings, string address);
}