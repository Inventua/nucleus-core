using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nucleus.Data.EntityFramework;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// DbContext options configuration class for use with Entity-Framework tools only.
	/// </summary>
	/// <typeparam name="TDataProvider"></typeparam>
	public class EFToolsDbContextConfigurator<TDataProvider> : DbContextConfigurator<TDataProvider>
		where TDataProvider : Nucleus.Data.Common.DataProvider
	{
		//private IOptions<DatabaseOptions> DatabaseOptions { get; }
		//private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		/// <param name="folderOptions"></param>
		public EFToolsDbContextConfigurator()//IOptions<DatabaseOptions> databaseOptions, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
		{
			//this.DatabaseOptions = databaseOptions;
			//this.FolderOptions = folderOptions;
		}

		/// <summary>
		/// Configure the DbContextOptionsBuilder for Sqlite
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public override Boolean Configure(DbContextOptionsBuilder options)
		{

			//DatabaseConnectionOption connectionOption = this.DatabaseOptions.Value.GetDatabaseConnection(typeof(TDataProvider).GetDefaultSchemaName());

			//if (connectionOption != null)
			//{
			//options.UseSqlite("Filename=:memory:");
			options.UseSqlite();
			return true;
			//}

			//return false;
			//return true;
		}
	}
}
