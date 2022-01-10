using System;
using System.Collections.Generic;
using System.Text;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Abstractions;

namespace Nucleus.Core.DataProviders.SQLite
{
	/// <summary>
	/// Session data provider options class.  
	/// </summary>
	/// <remarks>
	/// This class automatically generates and specifies the path of the database file.  It contains no user-configurable options.
	/// </remarks>
	public class SQLiteDataProviderOptions : Abstractions.SQLiteDataProviderOptions
	{
		internal const string Section = "SQLiteDataProviderOptions";
				
		/// <summary>
		/// Constructor
		/// </summary>
		public SQLiteDataProviderOptions()
		{	
			this.FileName =  System.IO.Path.Combine(Folders.GetDataFolder("Data"), "Nucleus.db");
		}
	}
}
