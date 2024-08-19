using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nucleus.Data.EntityFramework;

/// <summary>
/// The Entity Framework implementation for some database types do not store time zone information / set the DateTime.Kind to 
/// Unspecified. This causes problems when converting values to "Local".
/// This converter ensures that date/time values are converted to UTC when they are saved to the database, and that when Date/time 
/// values are read from the database, the .Kind property is set to Utc.
/// </summary>
internal class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
  public DateTimeUtcConverter() : base
  (
    value => ConvertTo(value),
    value => ConvertFrom(value)
  ) { }

  /// <summary>
  /// Convert a value before writing it to the database.
  /// </summary>
  /// <param name="value"></param>
  /// <returns>
  /// This function keeps a datetime with a value of DateTime.MinValue as-is, but converts all other values to UTC. If the 
  /// value is already UTC, .ToUniversalTime will not change it.  <seealso href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs#L824"/>
  /// </returns>
  private static DateTime ConvertTo(DateTime value)
  {
    // preserve DateTime.MinValue, which is often used as an alternative to null
    if (value == DateTime.MinValue) return value;

    switch (value.Kind)
    {
      case DateTimeKind.Local: 
        // convert local dates to UTC
        return value.ToUniversalTime();

      case DateTimeKind.Utc: 
        // date is already UTC, return as-is
        return value;

      case DateTimeKind.Unspecified:
        // assume date/time values with an unspecified Kind are already UTC
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);        
    }

    // catch-all
    return value;
  }

  /// <summary>
  /// Convert a value after reading it from the database
  /// </summary>
  /// <param name="value"></param>
  /// <returns>
  /// This function sets the "Kind" property to Utc without changing the value of the date/time, unless the value is DateTime.MinValue. If the 
  /// value is DateTime.MinValue, the value is returned as-is.
  /// </returns>
  private static DateTime ConvertFrom(DateTime value)
  {
    // preserve DateTime.MinValue, which is often used as an alternative to null
    if (value == DateTime.MinValue) return value;

    switch (value.Kind)
    {
      case DateTimeKind.Local: 
        // convert local file to UTC
        return value.ToUniversalTime();

      case DateTimeKind.Utc:
        // this case would only happen if Entity-framework starts supporting timezones in dates - but if we get a 
        // date in UTC, preserve it as-is
        return value;  
              
      case DateTimeKind.Unspecified:
        // assume date/time values with Kind=Unspecified are already UTC and set the Kind to UTC
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    // catch-all
    return value;
  }
}

internal class NullableDateTimeUtcConverter : ValueConverter<DateTime?, DateTime?>
{
  public NullableDateTimeUtcConverter()
    : base
    (
      value => ConvertTo(value),
      value => ConvertFrom(value)
    )
  {
  }

  /// <summary>
  /// Convert a value before writing it to the database.
  /// </summary>
  /// <param name="value"></param>
  /// <returns>
  /// This function keeps a null datetime as-is, but converts all other values to UTC. 
  /// </returns>

  private static DateTime? ConvertTo(DateTime? value)
  {
    // preseve nulls
    if (!value.HasValue) return value;

    switch (value.Value.Kind)
    {
      case DateTimeKind.Local: 
        // convert local date to UTC
        return value.Value.ToUniversalTime();

      case DateTimeKind.Utc: 
        // date is already UTC, return as-is
        return value;

      case DateTimeKind.Unspecified:
        // assume date/time values with an unspecified Kind are already UTC. This is the opposite of what ToUniversalTime()
        // normally does, which is assume that the date is local. Kind=Unspecified could happen when a date/time is deserialized
        // from MVC or a web API
        return DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    // catch-all
    return value;
  }

  /// <summary>
  /// Convert a value after reading it from the database
  /// </summary>
  /// <param name="value"></param>
  /// <returns>
  /// When Entity framework deserializes DateTime values, it sets the "Kind" to Unspecified.  This function sets the "Kind" property
  /// to Utc without changing the value of the date/time, if the value is not null. Null values are preserved.
  /// </returns>
  private static DateTime? ConvertFrom(DateTime? value)
  {
    // preserve null values
    if (!value.HasValue) return value;

    switch (value.Value.Kind)
    {
      case DateTimeKind.Local:
        // this case would only happen if Entity-framework starts supporting timezones in dates
        return value.Value.ToUniversalTime();

      case DateTimeKind.Utc:
        // this case would only happen if Entity-framework starts supporting timezones in dates
        return value;  

      case DateTimeKind.Unspecified: 
        // assume date/time values with an unspecified Kind are UTC
        return DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    // catch-all
    return value;
  }
}
