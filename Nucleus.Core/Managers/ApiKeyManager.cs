using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="ApiKey"/>s.
	/// </summary>
	public class ApiKeyManager : IApiKeyManager
	{
		private ICacheManager CacheManager { get; }
		private IDataProviderFactory DataProviderFactory { get; }

		public ApiKeyManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="ApiKey"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="ApiKey"/> to the database.  Call <see cref="Save(apiKey)"/> to save the Api Key.
		/// </remarks>
		public Task<ApiKey> CreateNew()
		{
			return Task.FromResult(new ApiKey() { Secret = GenerateSecret(192) });
		}

		// From https://stackoverflow.com/questions/730268/unique-random-string-generation
		private string GenerateSecret(int length, string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
		{
			if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be less than zero.");
			if (string.IsNullOrEmpty(allowedChars)) throw new ArgumentException("allowedChars may not be empty.");

			const int byteSize = 0x100;
			var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
			if (byteSize < allowedCharSet.Length) throw new ArgumentException(String.Format("allowedChars may contain no more than {0} characters.", byteSize));

			using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
			{
				var result = new StringBuilder();
				var buf = new byte[128];
				while (result.Length < length)
				{
					rng.GetBytes(buf);
					for (var i = 0; i < buf.Length && result.Length < length; ++i)
					{
						// Divide the byte into allowedCharSet-sized groups. If the
						// random value falls into the last group and the last group is
						// too small to choose from the entire allowedCharSet, ignore
						// the value in order to avoid biasing the result.
						var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
						if (outOfRangeStart <= buf[i]) continue;
						result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
					}
				}
				return result.ToString();
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="ApiKey"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ApiKey> Get(Guid id)
		{
			return await this.CacheManager.ApiKeyCache().GetAsync(id, async id =>
			{
				using (IApiKeyDataProvider provider = this.DataProviderFactory.CreateProvider<IApiKeyDataProvider>())
				{
					return await provider.GetApiKey(id);
				}
			});
		}

		/// <summary>
		/// List all <see cref="ApiKey"/>s.
		/// </summary>
		/// <returns></returns>
		public async Task<IEnumerable<ApiKey>> List()
		{
			using (IApiKeyDataProvider provider = this.DataProviderFactory.CreateProvider<IApiKeyDataProvider>())
			{
				return await provider.ListApiKeys();
			}
		}

		/// <summary>
		/// List paged <see cref="ApiKey"/>s for the specified site.
		/// </summary>
		/// <returns></returns>
		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<ApiKey>> List(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			using (IApiKeyDataProvider provider = this.DataProviderFactory.CreateProvider<IApiKeyDataProvider>())
			{
				return await provider.ListApiKeys(pagingSettings);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="ApiKey"/>.
		/// </summary>
		/// <param name="ApiKey"></param>
		public async Task Save(ApiKey apiKey)
		{
			using (IApiKeyDataProvider provider = this.DataProviderFactory.CreateProvider<IApiKeyDataProvider>())
			{
				await provider.SaveApiKey(apiKey);
				this.CacheManager.ApiKeyCache().Remove(apiKey.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="ApiKey"/> from the database.
		/// </summary>
		/// <param name="ApiKey"></param>
		public async Task Delete(ApiKey apiKey)
		{
			using (IApiKeyDataProvider provider = this.DataProviderFactory.CreateProvider<IApiKeyDataProvider>())
			{
				await provider.DeleteApiKey(apiKey);
				this.CacheManager.ApiKeyCache().Remove(apiKey.Id);
			}
		}

	}
}
