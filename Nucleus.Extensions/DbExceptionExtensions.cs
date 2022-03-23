using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension for handling exceptions.
	/// </summary>
	public static class DbExceptionExtensions
	{
		/// <summary>
		/// Parse a DbException and return a friendly message in a DataProviderException, or return the original exception if the message is not recognized.
		/// </summary>
		/// <param name="ex"></param>
		public static Exception Parse(this Exception ex)
		{
			if (ex != null)
			{
				if (ex.InnerException != null)
				{
					return ParseException(ex.InnerException);
				}
				else
				{
					return ParseException(ex);
				}
			}
			return ex;
		}

		private static Exception ParseException(Exception ex)
		{
			if (ex != null && ex is System.Data.Common.DbException)
			{
				if (ex.Message.Contains("foreign key constraint fail", StringComparison.OrdinalIgnoreCase))
				{
					if (ex.Message.Contains("FK_Pages_ParentId", StringComparison.OrdinalIgnoreCase))
					{
						return new DataProviderException("Cannot delete a page with child pages. Please delete or reassign your child pages first.", ex);
					}
				}
			}

			return ex;
		}

	}
}
