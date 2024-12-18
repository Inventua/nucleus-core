using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nucleus.Modules.Maps.Models;

public class AzureGeocodingResponse
{
  [JsonPropertyName("type")]
  public string Type { get; set; }

  public IList<GeocodingFeature> Features { get; set; }

  public class GeocodingFeature
  {
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; }

    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; }

    //[JsonPropertyName("geometry")]
    //public Geometry Geometry { get; set; }

    //[JsonPropertyName("place_id")]
    //public string PlaceId { get; set; }

    //[JsonPropertyName("plus_code")]
    //public string PlusCode { get; set; }

  }

  public class Properties
  {
    [JsonPropertyName("address")]
    public Address Address { get; set; }


    [JsonPropertyName("geocodePoints")]
    public IList<GeocodePoint> GeocodePoints { get; set; }


  }

  public class Address
  {
    [JsonPropertyName("countryRegion")]
    public CountryRegion CountryRegion { get; set; }

    [JsonPropertyName("adminDistricts")]
    public IList<AdminDistrict> AdminDistricts { get; set; }

    [JsonPropertyName("formattedAddress")]
    public string FormattedAddress { get; set; }

    [JsonPropertyName("locality")]
    public string Locality { get; set; }

    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; }

    [JsonPropertyName("addressLine")]
    public string AddressLine { get; set; }
  }

  public class CountryRegion
  {
    [JsonPropertyName("name")]
    public string Name { get; set; }
  }

  public class AdminDistrict
  {
    [JsonPropertyName("shortName")]
    public string ShortName { get; set; }
  }

  public class GeocodePoint
  {
    [JsonPropertyName("coordinates")]
    public string[] Coordinates { get; set; }
    //public Coordinates Location { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
  }

  public class Coordinates
  {
    public double Latitude { get; set; }

    public double Longitude { get; set; }
  }
}
