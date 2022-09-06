using System;
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
		/// Convert a DateTime from UTC to local time and output as a string in short date+short time format.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timezone"></param>
		/// <returns></returns>
		public static string FormatDate(this DateTime? value, TimeZoneInfo timezone)
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
		/// <param name="timezone"></param>
		/// <returns></returns>
		public static string FormatDate(this DateTime value, TimeZoneInfo timezone)
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

				if (value.TimeOfDay == TimeSpan.Zero)
				{
					return $"{System.TimeZoneInfo.ConvertTime(value, System.TimeZoneInfo.Utc, timezone).ToShortDateString()}";
				}
				else
				{
					return $"{System.TimeZoneInfo.ConvertTime(value, System.TimeZoneInfo.Utc, timezone).ToString("g")}";
				}
			}
		}

		/// <summary>
		/// Convert a DateTime from UTC to local time and output as a string in short date format.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timezone"></param>
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
			StringBuilder result = new();
			try
			{
				if (value.Days > 0)
				{
					result.Append($"{value.Days} day{(value.Days==1 ? "" : "s")}, ");
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
			if (!value.HasValue) return "";

			StringBuilder result = new();
			try
			{
				if (value.Value.Days > 0)
				{
					result.Append($"{value.Value.Days} day{(value.Value.Days == 1 ? "" : "s")}, ");
				}
				if (value.Value.Hours > 0)
				{
					result.Append($"{value.Value.Hours} hour{(value.Value.Hours == 1 ? "" : "s")}, ");
				}
				if (value.Value.Minutes > 0)
				{
					result.Append($"{value.Value.Minutes} minute{(value.Value.Minutes == 1 ? "" : "s")}, ");
				}
				result.Append($"{value.Value.Seconds} second{(value.Value.Seconds == 1 ? "" : "s")}");

				return result.ToString();
			}
			catch (Exception)
			{

			}

			return value.ToString();
		}
	}
}
