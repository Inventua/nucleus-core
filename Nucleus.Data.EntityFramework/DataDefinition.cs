using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// Specifies database migration operations.
	/// </summary>
	/// <remarks>
	/// This class is typically deserialized from a migration operations script file (json).
	/// </remarks>
	internal class DataDefinition
	{
		/// <summary>
		/// The name of the application or extension schema.  This is not the database schema (like dbo), it is a unique identifier
		/// for a set of migrations.  It is typically set to the root namespace of your module.
		/// </summary>
		/// <example>
		/// Nucleus.Core
		/// Nucleus.Core.Modules.Documents
		/// MyOrganization.Modules.MyModule
		/// </example>
		public string SchemaName { get; set; }

		/// <summary>
		/// Script version.
		/// </summary>
		public System.Version Version { get; set; }

		/// <summary>
		/// A list of operations to execute.
		/// </summary>
		public IReadOnlyList<MigrationOperation> Operations { get; set; }

	}
}
