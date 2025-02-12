﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ViewFeatures
{
	/// <summary>
	/// Extensions which format the output of various types.
	/// </summary>
	static public class FormatExtensions
	{
    /// <summary>
    /// Enum used to specify timespan formats for <see cref="FormatTimeSpan(TimeSpan, TimespanFormats)" />.
    /// </summary>
    public enum TimespanFormats
    {
      /// <summary>
      /// Human-readable friendly format
      /// </summary>
      Friendly = 0,
      /// <summary>
      /// Format as time zone offset (+10, -5:30)
      /// </summary>
      TimeOffset = 1
    }

    /// <summary>
    /// Convert a DateTime from UTC to local time and output as a string in short date+short time format.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="timezone">Current user time zone.  Use this.Context.Request.GetUserTimeZone() to use the auto-detected time zone.</param>
    /// <returns></returns>
    public static string FormatDate(this DateTime? value, TimeZoneInfo timezone)
    {
      return FormatDate(value, timezone, false);
    }

    /// <summary>
    /// Convert a DateTime from UTC to local time and output as a string in short date+short or long time format. The <paramref name="useLongTimeFormat"/> 
    /// parameter controls the time format.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="timezone">Current user time zone.  Use this.Context.Request.GetUserTimeZone() to use the auto-detected time zone.</param>
    /// <param name="useLongTimeFormat">Specifies whether to use short or long time format.</param>
    /// <returns></returns>
    public static string FormatDate(this DateTime? value, TimeZoneInfo timezone, Boolean useLongTimeFormat)
		{
			if (value.HasValue)
			{
				return FormatDate(value.Value, timezone);
			}
			else
			{
				return "";
			}
		}


    /// <summary>
    /// Convert a DateTime from UTC to local time and output as a string in short date+short time format.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="timezone">Current user time zone.  Use this.Context.Request.GetUserTimeZone() to use the auto-detected time zone.</param>
    /// <returns></returns>
    public static string FormatDate(this DateTime value, TimeZoneInfo timezone)
    {
      return FormatDate(value, timezone, false);
    }

    /// <summary>
    /// Convert a DateTime from UTC to local time and output as a string in short date + short or long time format. The <paramref name="useLongTimeFormat"/> 
    /// parameter controls the time format.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="timezone">Current user time zone.  Use this.Context.Request.GetUserTimeZone() to use the auto-detected time zone.</param>
    /// <param name="useLongTimeFormat">Specifies whether to use short or long time format.</param>
    /// <returns></returns>
    public static string FormatDate(this DateTime value, TimeZoneInfo timezone, Boolean useLongTimeFormat)
		{
			if (value == DateTime.MinValue)
			{
				return "";
			}
			else
			{
				if (timezone == null)
				{
					timezone = TimeZoneInfo.Local;
				}

        // deserialized dates can have Kind=Unspecified, which breaks conversion to the specified time zone. We assume that dates
        // are Utc unless their .Kind is already DateTimeKind.Local
        DateTime utcValue = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc): value;

        // if the time component is 12:00:00 AM, display date only, otherwise include the time
        if (utcValue.TimeOfDay == TimeSpan.Zero)
        {
          return $"{TimeZoneInfo.ConvertTime(utcValue, timezone):d}";
        }
        else
        {
          if (useLongTimeFormat)
          {
            return $"{TimeZoneInfo.ConvertTime(utcValue, timezone):G}";
          }
          else
          {
            return $"{TimeZoneInfo.ConvertTime(utcValue, timezone):g}";
          }
        }
			}
		}

    /// <summary>
    /// Convert a DateTime from UTC to local time and output as a string in short date format.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="timezone">Current user time zone.  Use this.Context.Request.GetUserTimeZone() to use the auto-detected time zone.</param>
    /// <returns></returns>
    public static string FormatDate(this DateTimeOffset value, TimeZoneInfo timezone)
		{
			if (value == DateTimeOffset.MinValue)
			{
				return "";
			}
			else
			{
				return FormatDate(value.DateTime, timezone);
			}
		}


    /// <summary>
    /// Return a "friendly" string representation of a TimeSpan.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FormatTimeSpan(this TimeSpan value)
    {
      return FormatTimeSpan(value, TimespanFormats.Friendly);
    }

    /// <summary>
    /// Return a "friendly" string representation of a TimeSpan.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="format">Specifies output format.</param>
    /// <returns></returns>
    public static string FormatTimeSpan(this TimeSpan value, TimespanFormats format)
		{
			StringBuilder result = new();
			try
			{
        if (format == TimespanFormats.TimeOffset)
        {
          if (value.TotalSeconds > 0)
          {
            result.Append('+');
          }
          if (value.Hours != 0)
          {
            result.Append($"{value.Hours}");
          }
          if (value.Minutes != 0)
          {
            result.Append($":{value.Minutes}");
          }
        }
        else if (format == TimespanFormats.Friendly)
        {
          if (value.Days > 0)
          {
            result.Append($"{value.Days} day{(value.Days == 1 ? "" : "s")}, ");
          }
          if (value.Hours > 0)
          {
            result.Append($"{value.Hours} hour{(value.Hours == 1 ? "" : "s")}, ");
          }
          if (value.Minutes > 0)
          {
            result.Append($"{value.Minutes} minute{(value.Minutes == 1 ? "" : "s")}, ");
          }
          result.Append($"{value.Seconds} second{(value.Seconds == 1 ? "" : "s")}");
        }

				return result.ToString();
			}
			catch (Exception)
			{

			}

			return value.ToString();
		}

    /// <summary>
    /// Return a "friendly" string representation of a TimeSpan.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FormatTimeSpan(this TimeSpan? value)
    {
      return FormatTimeSpan(value, TimespanFormats.Friendly);
    }

    /// <summary>
    /// Return a "friendly" string representation of a TimeSpan.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="format">Specifies output format.</param>
    /// <returns></returns>
    public static string FormatTimeSpan(this TimeSpan? value, TimespanFormats format)
		{
			if (!value.HasValue) return "";

      return FormatTimeSpan(value.Value, format);      
		}
	}
}
