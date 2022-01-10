using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Nucleus.Data.EntityFramework
{
	internal class DatabaseProviderSpecificOperation : MigrationOperation
	{
		public string[] Include { get; set; }
		public string[] Exclude { get; set; }

		public MigrationOperation Operation { get; set; }

		/// <summary>
		/// Default constructor, required for deserialization.
		/// </summary>
		public DatabaseProviderSpecificOperation() { }

		public Boolean IsValidFor(string databaseProviderName)
		{
			string shortName;
			int position = databaseProviderName.LastIndexOf('.');

			if (position < 0)
			{
				shortName = databaseProviderName;
			}
			else
			{
				shortName = databaseProviderName.Substring(position+1);
			}

			// If the provider name is in the exclude list, the command is not valid
			if (this.Exclude != null && this.Exclude.Contains(shortName, StringComparer.OrdinalIgnoreCase))
			{
				return false;
			}

			// If the provider name is in the include list, the command is valid
			if (this.Include != null && this.Include.Contains(shortName, StringComparer.OrdinalIgnoreCase))
			{
				return true;
			}

			// If the provider name is not in the exclude list, but an exclude list was specified, and no include 
			// list was specified the command is valid
			if (this.Exclude != null || this.Include == null)
			{
				return true;
			}

			return false;
		}
	}
}
