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
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.Managers
{
	public class ContainerManager : IContainerManager
	{
		private IDataProviderFactory DataProviderFactory { get; }

		public ContainerManager(IDataProviderFactory dataProviderFactory)
		{
			this.DataProviderFactory = dataProviderFactory;
		}

    private ContainerDefinition NormalizeRelativePath(ContainerDefinition containerDefinition)
    {
      if (containerDefinition != null)
      {
        containerDefinition.RelativePath = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(containerDefinition.RelativePath);
      }
      return containerDefinition;
    }

    public async Task<List<ContainerDefinition>> List()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
        List<ContainerDefinition> results = await provider.ListContainerDefinitions();
        foreach(ContainerDefinition result in results)
        {
          NormalizeRelativePath(result);
        }
        return results;
			}
		}
	}
}
