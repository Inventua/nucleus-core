using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System.Text.Json.Serialization;

namespace Nucleus.Modules.Maps.Models;

public class GeocodingLocation 
{
  public string Address { get; set; }
  
  public IList<string> LocationTypes { get; set; }

  public Coordinates Geometry { get; set; } = new();

  public Viewport Viewport { get; set; } = new();
}


public class Viewport
{
  public Coordinates TopLeft { get; set; } = new();

  public Coordinates BottomRight { get; set; } = new();
}

public class Coordinates
{
  public double Latitude { get; set; }

  public double Longitude { get; set; }
}