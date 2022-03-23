using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	/// <summary>
	/// 
	/// </summary>
	public class DataProviderException : Exception
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="ex"></param>
		public DataProviderException(string message, Exception ex) : base(message, ex) 
		{
			
		}

	}
}
