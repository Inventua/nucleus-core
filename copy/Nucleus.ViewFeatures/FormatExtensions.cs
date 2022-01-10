using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ViewFeatures
{
	static public class FormatExtensions
	{
		public static string FormatDate(this DateTime value, Boolean fromUTC)
		{
			if (value == DateTime.MinValue)
			{
				return "";
			}
			else
			{
				if (fromUTC)
				{
					value = value.ToLocalTime();
				}

				return $"{value.ToShortDateString()} {value.ToShortTimeString()}";
			}
		}

		public static string FormatDate(this DateTimeOffset value, Boolean fromUTC)
		{
			if (value == DateTimeOffset.MinValue)
			{
				return "";
			}
			else
			{
				if (fromUTC)
				{
					value = value.ToLocalTime();
				}

				return $"{value.DateTime.ToShortDateString()} {value.DateTime.ToShortTimeString()}";
			}
		}
	}
}
