using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
  /// Provides create, read, update and delete functionality for the <see cref="ExtensionsStoreSettings"/> class.
  internal interface IExtensionsStoreDataProvider : IDisposable
	{
		abstract Task SaveExtensionsStoreSettings(ExtensionsStoreSettings settings);
		abstract Task<ExtensionsStoreSettings> GetExtensionsStoreSettings(string storeUrl);
		
	}
}
