using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using $nucleus_extension_namespace$.DataProviders;
using $nucleus_extension_namespace$.Models;

namespace $nucleus_extension_namespace$
{
	/// <summary>
	/// Provides functions to manage database data.
	/// </summary>
	public class $nucleus_extension_name$Manager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public $nucleus_extension_name$Manager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="$nucleus_extension_modelname$"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="$nucleus_extension_modelname$"/> is not saved to the database until you call <see cref="Save(PageModule, $nucleus_extension_modelname$)"/>.
		/// </remarks>
		public $nucleus_extension_modelname$ CreateNew()
		{
			$nucleus_extension_modelname$ result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="$nucleus_extension_modelname$"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<$nucleus_extension_modelname$> Get(Guid id)
		{
			return await this.CacheManager.$nucleus_extension_modelname$sCache().GetAsync(id, async id =>
			{
				using (I$nucleus_extension_name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus_extension_name$DataProvider>())
				{
					return await provider.Get(id);
				}
			});
		}

		/// <summary>
		/// Delete the specified <see cref="$nucleus_extension_modelname$"/> from the database.
		/// </summary>
		/// <param name="$nucleus_extension_modelname$"></param>
		public async Task Delete($nucleus_extension_modelname$ $nucleus_extension_modelname_camelcase$)
		{
			using (I$nucleus_extension_name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus_extension_name$DataProvider>())
			{
				await provider.Delete($nucleus_extension_modelname_camelcase$);
				this.CacheManager.$nucleus_extension_modelname$sCache().Remove($nucleus_extension_modelname_camelcase$.Id);
			}
		}

		/// <summary>
		/// List all <see cref="$nucleus_extension_modelname$"/>s within the specified site.
		/// </summary>
		/// <param name="module"></param>
		/// <returns></returns>
		public async Task<IList<$nucleus_extension_modelname$>> List(PageModule module)
		{
			using (I$nucleus_extension_name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus_extension_name$DataProvider>())
			{
				return await provider.List(module);
			}
		}

		/// <summary>
		/// Create or update a <see cref="$nucleus_extension_modelname$"/>.
		/// </summary>
		/// <param name="module"></param>
		/// <param name="$nucleus_extension_modelname$"></param>
		public async Task Save(PageModule module, $nucleus_extension_modelname$ $nucleus_extension_modelname_camelcase$)
		{
			using (I$nucleus_extension_name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus_extension_name$DataProvider>())
			{
				await provider.Save(module, $nucleus_extension_modelname_camelcase$);
				this.CacheManager.$nucleus_extension_modelname$sCache().Remove($nucleus_extension_modelname_camelcase$.Id);				
			}
		}

	}
}
