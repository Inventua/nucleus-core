using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="ApiKey"/>, class.
	internal interface IApiKeyDataProvider : IDisposable//
	{
		abstract Task<IEnumerable<ApiKey>> ListApiKeys();
		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<ApiKey>> ListApiKeys(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		abstract Task<ApiKey> GetApiKey(Guid apiKeyId);
		abstract Task SaveApiKey(ApiKey apiKey);
		abstract Task DeleteApiKey(ApiKey apiKey);
	}
}
