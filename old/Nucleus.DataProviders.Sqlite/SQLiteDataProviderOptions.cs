using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Core.DataProviders.Abstractions
{
	/// <summary>
	/// Used to configure data SQLite provider.  
	/// </summary>
	/// <remarks>
	/// SQLite Data provider implementations can inherit this class and set the FileName property, or can use the DI options methods
	/// to read the filename from a configuration file.
	/// </remarks>
	public abstract class SQLiteDataProviderOptions
	{
		/// <value>
		/// SQLite database path and file name
		/// </value>
		public string FileName { get; set; }

	}
}
