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
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseOptions"></param>
		public EFToolsDbContextConfigurator(IOptions<Nucleus.Abstractions.Models.Configuration.DatabaseOptions> options) : base(options) { }

		/// <summary>
		/// Configure the DbContextOptionsBuilder for Sqlite
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public override Boolean Configure(DbContextOptionsBuilder options)
		{
			options.UseSqlite();
			return true;
		}

		/// <inheritdoc/>
		public override void ParseException(DbUpdateException exception)
		{
			// no implementation
		}

	}
}
