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
		/// <param name="fromUTC"></param>
		/// <returns></returns>
		public static string FormatDate(this DateTime? value, Boolean fromUTC)
		{
			if (value.HasValue)
			{
				return FormatDate(value.Value, fromUTC);
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
		/// <param name="fromUTC"></param>
		/// <returns></returns>
		public static string FormatDate(this DateTime value, Boolean fromUTC)
		{
			if (value == DateTime.MinValue)
			{
				return "";
			}
			else
			{
				if (value.TimeOfDay == TimeSpan.Zero)
				{
					return $"{(fromUTC ? value.ToLocalTime() : value).ToShortDateString()}";
				}
				else
				{
					return $"{(fromUTC ? value.ToLocalTime() : value).ToString("g")}";
				}
			}
		}

		/// <summary>
		/// Convert a DateTime from UTC to local time and output as a string in short date format.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="fromUTC"></param>
		/// <returns></returns>
		public static string FormatDate(this DateTimeOffset value, Boolean fromUTC)
		{
			if (value == DateTimeOffset.MinValue)
			{
				return "";
			}
			else
			{
				//if (fromUTC)
				//{
				//	value = value.ToLocalTime();
				//}

				return FormatDate(value.DateTime, fromUTC);
				//return $"{value.DateTime.ToShortDateString()} {value.DateTime.ToShortTimeString()}";
			}
		}
	}
}
