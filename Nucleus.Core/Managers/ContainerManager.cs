using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core.Managers
{
	public class ContainerManager : IContainerManager
	{
		private IDataProviderFactory DataProviderFactory { get; }

		public ContainerManager(IDataProviderFactory dataProviderFactory)
		{
			this.DataProviderFactory = dataProviderFactory;
		}

		public async Task<List<Nucleus.Abstractions.Models.ContainerDefinition>> List()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return await provider.ListContainerDefinitions();
			}
		}
	}
}
