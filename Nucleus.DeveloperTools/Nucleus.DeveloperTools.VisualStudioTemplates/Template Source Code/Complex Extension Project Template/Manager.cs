using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using $nucleus.extension.namespace$.DataProviders;
using $nucleus.extension.namespace$.Models;

namespace $nucleus.extension.namespace$
{
	/// <summary>
	/// Provides functions to manage database data.
	/// </summary>
	public class $nucleus.extension.name$Manager
	{
		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }

		public $nucleus.extension.name$Manager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="$nucleus.extension.model_class_name$"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="$nucleus.extension.model_class_name$"/> is not saved to the database until you call <see cref="Save(PageModule, $nucleus.extension.model_class_name$)"/>.
		/// </remarks>
		public $nucleus.extension.model_class_name$ CreateNew()
		{
			$nucleus.extension.model_class_name$ result = new();

			return result;
		}

		/// <summary>
		/// Retrieve an existing <see cref="$nucleus.extension.model_class_name$"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<$nucleus.extension.model_class_name$> Get(Guid id)
		{
			return await this.CacheManager.$nucleus.extension.model_class_name$sCache().GetAsync(id, async id =>
			{
				using (I$nucleus.extension.name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus.extension.name$DataProvider>())
				{
					return await provider.Get(id);
				}
			});
		}

		/// <summary>
		/// Delete the specified <see cref="$nucleus.extension.model_class_name$"/> from the database.
		/// </summary>
		/// <param name="$nucleus.extension.model_class_name$"></param>
		public async Task Delete($nucleus.extension.model_class_name$ $nucleus.extension.model_class_name.camelcase$)
		{
			using (I$nucleus.extension.name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus.extension.name$DataProvider>())
			{
				await provider.Delete($nucleus.extension.model_class_name.camelcase$);
				this.CacheManager.$nucleus.extension.model_class_name$sCache().Remove($nucleus.extension.model_class_name.camelcase$.Id);
			}
		}

		/// <summary>
		/// List all <see cref="$nucleus.extension.model_class_name$"/>s within the specified site.
		/// </summary>
		/// <param name="module"></param>
		/// <returns></returns>
		public async Task<IList<$nucleus.extension.model_class_name$>> List(PageModule module)
		{
			using (I$nucleus.extension.name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus.extension.name$DataProvider>())
			{
				return await provider.List(module);
			}
		}

		/// <summary>
		/// Create or update a <see cref="$nucleus.extension.model_class_name$"/>.
		/// </summary>
		/// <param name="module"></param>
		/// <param name="$nucleus.extension.model_class_name$"></param>
		public async Task Save(PageModule module, $nucleus.extension.model_class_name$ $nucleus.extension.model_class_name.camelcase$)
		{
			using (I$nucleus.extension.name$DataProvider provider = this.DataProviderFactory.CreateProvider<I$nucleus.extension.name$DataProvider>())
			{
				await provider.Save(module, $nucleus.extension.model_class_name.camelcase$);
				this.CacheManager.$nucleus.extension.model_class_name$sCache().Remove($nucleus.extension.model_class_name.camelcase$.Id);				
			}
		}

	}
}
