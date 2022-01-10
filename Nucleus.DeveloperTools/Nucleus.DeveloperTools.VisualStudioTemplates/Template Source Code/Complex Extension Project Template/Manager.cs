using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Data.Common;
using $nucleus_extension_namespace$.DataProviders;
using $nucleus_extension_namespace$.Models;

namespace $nucleus_extension_namespace$
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="$nucleus_extension_name$"/>s.
	/// </summary>
	public class $nucleus_extension_name$Manager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public $nucleus_extension_name$Manager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;

			this.CacheManager.Add<Guid, $nucleus_extension_name$> (new Nucleus.Abstractions.Models.Configuration.CacheOption());
		}

		/// <summary>
		/// Create a new <see cref="$nucleus_extension_name$"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="$nucleus_extension_name$"/> is not saved to the database until you call <see cref="Save(PageModule, $nucleus_extension_name$)"/>.
		/// </remarks>
		public $nucleus_extension_name$ CreateNew()
		{
			$nucleus_extension_name$ result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="$nucleus_extension_name$"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public $nucleus_extension_name$ Get(Guid id)
		{
			return this.CacheManager.$nucleus_extension_name$Cache().Get(id, id =>
			{
				using (I$nucleus_extension_name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus_extension_name$DataProvider>())
				{
					return provider.Get(id);
				}
			});
		}

		/// <summary>
		/// Delete the specified <see cref="$nucleus_extension_name$"/> from the database.
		/// </summary>
		/// <param name="$nucleus_extension_name$"></param>
		public void Delete($nucleus_extension_name$ $nucleus_extension_name_lcase$)
		{
			using (I$nucleus_extension_name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus_extension_name$DataProvider>())
			{
				provider.Delete($nucleus_extension_name_lcase$);
				this.CacheManager.$nucleus_extension_name$Cache().Remove($nucleus_extension_name_lcase$.Id);
			}
		}

		/// <summary>
		/// List all <see cref="$nucleus_extension_name$"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IList<$nucleus_extension_name$> List(PageModule module)
		{
			using (I$nucleus_extension_name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus_extension_name$DataProvider>())
			{
				return provider.List(module);
			}
		}

		/// <summary>
		/// Create or update a <see cref="$nucleus_extension_name$"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="$nucleus_extension_name$"></param>
		public void Save(PageModule module, $nucleus_extension_name$ $nucleus_extension_name_lcase$)
		{
			using (I$nucleus_extension_name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus_extension_name$DataProvider>())
			{
				provider.Save(module, $nucleus_extension_name_lcase$);
				this.CacheManager.$nucleus_extension_name$Cache().Remove($nucleus_extension_name_lcase$.Id);				
			}
		}

	}
}
