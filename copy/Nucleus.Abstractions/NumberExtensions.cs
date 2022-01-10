using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	public static class NumberExtensions
	{
		public static string FormatSize(this long size)
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
