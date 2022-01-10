using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// Data provider factory base class.  A data provider factory retrieves data provider interface instances from the dependency injection container.
	/// </summary>
	public interface IDataProviderFactory
	{
		/// <summary>
		/// Create a new instance of the specified data provider interface
		/// </summary>
		/// <typeparam name="TDataProvider"></typeparam>
		/// <returns>A data provider instance of the specified type.</returns>
		public TDataProvider CreateProvider<TDataProvider>(); 
	}
}
