using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Data provider configuration interface.
	/// </summary>
	public interface IConfigureDataProvider
	{
		/// <summary>
		/// Add data provider objects to the service collection for the data provider specified by TDataProvider if configuration 
		/// contains an entry specifying that the data provider uses SqlServer.  This overload allows callers to specify their schema name instead 
		/// if using the default.
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <param name="services"></param>
		/// <param name="options"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public Boolean AddDataProvider<TDataProvider>(IServiceCollection services, DatabaseOptions options, string schemaName)
			where TDataProvider : Nucleus.Data.Common.DataProvider;
	}
}
