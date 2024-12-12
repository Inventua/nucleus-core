using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System.Text.Json.Serialization;

namespace Nucleus.Modules.Maps.Models;

public class GoogleGeocodingResponse
{

  ////public enum AddressComponentTypes
  ////{
  ////  //    street_address indicates a precise street address.
  ////  //route indicates a named route (such as "US 101").
  ////  //intersection indicates a major intersection, usually of two major roads.
  ////  //political indicates a political entity.Usually, this type indicates a polygon of some civil administration.
  ////  //country indicates the national political entity, and is typically the highest order type returned by the Geocoder.
  ////  //administrative_area_level_1 indicates a first-order civil entity below the country level.Within the United States, these administrative levels are states.Not all nations exhibit these administrative levels.In most cases, administrative_area_level_1 short names will closely match ISO 3166-2 subdivisions and other widely circulated lists; however this is not guaranteed as our geocoding results are based on a variety of signals and location data.
  ////  //administrative_area_level_2 indicates a second-order civil entity below the country level.Within the United States, these administrative levels are counties.Not all nations exhibit these administrative levels.
  ////  //administrative_area_level_3 indicates a third-order civil entity below the country level.This type indicates a minor civil division.Not all nations exhibit these administrative levels.
  ////  //administrative_area_level_4 indicates a fourth-order civil entity below the country level.This type indicates a minor civil division.Not all nations exhibit these administrative levels.
  ////  //administrative_area_level_5 indicates a fifth-order civil entity below the country level.This type indicates a minor civil division.Not all nations exhibit these administrative levels.
  ////  //administrative_area_level_6 indicates a sixth-order civil entity below the country level.This type indicates a minor civil division.Not all nations exhibit these administrative levels.
  ////  //administrative_area_level_7 indicates a seventh-order civil entity below the country level.This type indicates a minor civil division.Not all nations exhibit these administrative levels.
  ////  //colloquial_area indicates a commonly-used alternative name for the entity.
  ////  //locality indicates an incorporated city or town political entity.
  ////  //sublocality indicates a first-order civil entity below a locality. For some locations may receive one of the additional types: sublocality_level_1 to sublocality_level_5.Each sublocality level is a civil entity.Larger numbers indicate a smaller geographic area.
  ////  //neighborhood indicates a named neighborhood.
  ////  //premise indicates a named location, usually a building or collection of buildings with a common name.
  ////  //subpremise indicates an addressable entity below the premise level, such as an apartment, unit, or suite.
  ////  //plus_code indicates an encoded location reference, derived from latitude and longitude.Plus codes can be used as a replacement for street addresses in places where they do not exist (where buildings are not numbered or streets are not named). See https://plus.codes for details.
  ////  //postal_code indicates a postal code as used to address postal mail within the country.
  ////  //natural_feature indicates a prominent natural feature.
  ////  //airport indicates an airport.
  ////  //park indicates a named park.
  ////  //point_of_interest
  ////}

  ////public enum LocationTypes
  ////{
  ////  /// <summary>
  ////  /// Based on location information accurate down to street address precision.
  ////  /// </summary>
  ////  Rooftop,
  ////  /// <summary>
  ////  /// Approximate result based on two precise points (eg. intersections).
  ////  /// </summary>
  ////  Range_Interpolated,
  ////  /// <summary>
  ////  /// Geometric center result such as a polyline (street) or polygon (region).
  ////  /// </summary>
  ////  Geometric_Center,
  ////  /// <summary>
  ////  /// Approximate result that does not correspond to the other precision types.
  ////  /// </summary>
  ////  Approximate
  ////}

  ////public enum StatusCodes
  ////{
  ////  Ok,
  ////  ZeroResults,
  ////  OverDailyLimit,
  ////  OverQueryLimit,
  ////  RequestDenied,
  ////  InvalidRequest,
  ////  UnknownError
  ////}

  [JsonPropertyName("results")]
  public IList<GeocodingResult> Results { get; set; }

  [JsonPropertyName("status")]
  public string StatusCode { get; set; }

  public class GeocodingResult
  {
    [JsonPropertyName("address_components")]
    public IList<AddressComponent> AddressComponents { get; set; }

    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; }

    [JsonPropertyName("geometry")]
    public Geometry Geometry { get; set; }

    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; }

    ////[JsonPropertyName("plus_code")]
    ////public string PlusCode { get; set; }

    [JsonPropertyName("types")]
    public IList<string> Types { get; set; }
  }

  public class AddressComponent
  {
    [JsonPropertyName("long_name")]
    public string LongName { get; set; }

    [JsonPropertyName("short_name")]
    public string ShortName { get; set; }

    [JsonPropertyName("types")]
    public IList<string> Types { get; set; }
    //public AddressComponentTypes Types { get; set; }
  }

  public class Geometry
  {
    [JsonPropertyName("location")]
    public Coordinates Location { get; set; }

    [JsonPropertyName("lcoation_type")]
    public string LocationType { get; set; }

    [JsonPropertyName("viewport")]
    public Viewport Viewport { get; set; }
  }

  public class Viewport
  {
    [JsonPropertyName("northeast")]
    public Coordinates NorthEast { get; set; }

    [JsonPropertyName("southwest")]
    public Coordinates SouthWest { get; set; }
  }

  public class Coordinates
  {
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double Longitude { get; set; }
  }
}
