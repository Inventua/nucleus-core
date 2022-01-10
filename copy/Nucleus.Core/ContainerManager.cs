using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;

namespace Nucleus.Core
{
	public class ContainerManager
	{
		private DataProviderFactory DataProviderFactory { get; }

		public ContainerManager(DataProviderFactory dataProviderFactory)
		{
			this.DataProviderFactory = dataProviderFactory;
		}

		public List<Nucleus.Abstractions.Models.ContainerDefinition> List()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.ListContainerDefinitions();
			}
		}
	}
}
