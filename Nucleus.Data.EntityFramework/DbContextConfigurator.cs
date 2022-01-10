using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// Database-specific EF implementations implement this interface in order to configure their EF DbContext.
	/// </summary>
	/// <typeparam name="TDataProvider"></typeparam>
	/// <remarks>
	/// Register your implementation of this class in the dependency injection service container, Nucleus will use it automatically. 
	/// </remarks>
	/// <example>
	/// <![CDATA[services.AddSingleton<Nucleus.Data.EntityFramework.IDbContextConfigurator<LinksDataProvider>, Nucleus.Data.Sqlite.SqliteOptionsConfigurator<LinksDataProvider>>();]]>
	/// </example>
	public abstract class DbContextConfigurator<TDataProvider> : DbContextConfigurator
	{	
	}

	/// <summary>
	/// This is the non-generic form of IDbContextConfigurator, used by <see cref="DbContext"/>.
	/// </summary>
	/// <remarks>
	/// Implementations of IDbContextConfigurator should use the generic form of <see cref="DbContextConfigurator{T}"/>.
	/// </remarks>
	public abstract class DbContextConfigurator
	{
		/// <summary>
		/// Configure entity-framework with the specified options.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <remarks>
		/// Entity-framework base database providers implement this method to call the initialization methods which are appropriate to 
		/// their specific implementation.
		/// </remarks>
		public abstract Boolean Configure(DbContextOptionsBuilder options);
	}
}
