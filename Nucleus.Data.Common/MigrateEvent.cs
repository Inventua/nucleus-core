using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.EventHandlers;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Type representing a migration script operation (event).  This class has no methods or properties, it is used as a key for the <see cref="ISystemEventHandler&lt;TModel, TEvent&gt;"/> class.
	/// </summary>
	public class Migrate { }

	/// <summary>
	/// Event data for a successful migrate (script execution) event
	/// </summary>
	public class MigrateEvent
	{
		/// <summary>
		/// Extension schema name.
		/// </summary>
		public string SchemaName { get; }

		/// <summary>
		/// File name of the script file which was execited/
		/// </summary>
		public string FileName { get; }

		/// <summary>
		/// Schema version before script execution.
		/// </summary>
		public System.Version FromVersion { get; }

		/// <summary>
		/// Schema version after script execution.
		/// </summary>
		public System.Version ToVersion { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="schemaName"></param>
		/// <param name="filename"></param>
		/// <param name="fromVersion"></param>
		/// <param name="toVersion"></param>
		public MigrateEvent(string schemaName, string filename, System.Version fromVersion, System.Version toVersion)
		{
			this.SchemaName = schemaName;
			this.FileName = filename;
			this.FromVersion = fromVersion;
			this.ToVersion = toVersion;
		}
	}
}
