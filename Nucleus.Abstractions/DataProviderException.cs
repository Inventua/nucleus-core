using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	/// <summary>
	///	Represents a parsed "friendly" error message derived from a DbException.
	/// </summary>
	/// <remarks>
	/// This type is intended for use by the Nucleus core only.
	/// </remarks>
	public class DataProviderException : Exception
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">Friendly error message.</param>
		/// <param name="ex">Original exception.</param>
		public DataProviderException(string message, Exception ex) : base(message, ex) 
		{
			
		}

	}
}
