using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension functions for numeric types.
	/// </summary>
	public static class NumberExtensions
	{
		/// <summary>
		/// Format a file size.
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public static string FormatFileSize(this long? size)
		{
			if (size.HasValue)
			{
				return FormatFileSize(size.Value);
			}
			else
			{
				return "";
			}
		}

		/// <summary>
		/// Format a file size.
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public static string FormatFileSize(this long size)
		{
			try
			{				
				if (size > (1024 * 1024))
					return (size / (double)(1024 * 1024)).ToString("#,##0.00 MB");
				else if (size > 1024)
					return (size / (double)1024).ToString("#,##0.00 KB");
				else
					return size.ToString("#0 bytes");				
			}
			catch (Exception)
			{
				
			}

			return "";
		}

	}

}
