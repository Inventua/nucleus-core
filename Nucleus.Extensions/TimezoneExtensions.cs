using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Http Request TimeZone extensions.
	/// </summary>
	public static class TimezoneExtensions
	{
		/// <summary>
		/// Return the request user's timezone.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		/// <remarks>
		/// The user's timezone UTC offset (in minutes) is stored in a cookie named timezone-offset by code in nucleus-shared.js
		/// </remarks>
		public static TimeZoneInfo GetUserTimeZone(this HttpRequest request)
		{
			if (request.Cookies["timezone-offset"] == null)
			{
				return TimeZoneInfo.Local;
			}
			else
			{
				return System.TimeZoneInfo.CreateCustomTimeZone("User Timezone", new TimeSpan(Convert.ToInt32(request.Cookies["timezone-offset"])), "User Timezone", "User Timezone");
			}
		}
	}
}
