using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Internal
{
	/// <summary>
	/// Represents a schema and version which has been applied to the database.
	/// </summary>
	public class Schema
	{
		/// <summary>
		/// Schema name which uniquely identifies the schema for a module or set of modules.
		/// </summary>
		/// <remarks>
		/// By convention, this is the root namespace for your module, but can be set to any value which can be expected to be unique.
		/// </remarks>
		public string SchemaName { get; set; }

		/// <summary>
		/// Schema version.
		/// </summary>
		public string SchemaVersion { get; set; }

	}
}
