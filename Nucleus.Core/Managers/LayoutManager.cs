using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.Options;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="LayoutDefinition"/>s.
	/// </summary>
	public class LayoutManager : ILayoutManager
	{
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

		private IDataProviderFactory DataProviderFactory { get; }

		public LayoutManager(IDataProviderFactory dataProviderFactory, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
		{
			this.DataProviderFactory = dataProviderFactory;
			this.FolderOptions = folderOptions;
		}

		/// <summary>
		/// Get the specifed <see cref="LayoutDefinition"/>.
		/// </summary>
		/// <returns></returns>
		public async Task<LayoutDefinition> Get(Guid id)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return await provider.GetLayoutDefinition(id);
			}
		}

		/// <summary>
		/// Returns a list of all installed <see cref="LayoutDefinition"/>s.
		/// </summary>
		/// <returns></returns>
		public async Task<IList<LayoutDefinition>> List()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return await provider.ListLayoutDefinitions();
			}
		}

		/// <summary>
		/// Scans a layout and returns a list of all panes in the layout.
		/// </summary>
		/// <param name="layout"></param>
		/// <returns></returns>
		/// <remarks>
		/// Panes are detected by scanning for <<![CDATA[RenderPaneAsync("PaneName")]]> statements in the layout.
		/// </remarks>
		public async Task<IEnumerable<string>> ListLayoutPanes(LayoutDefinition layout)
		{
			string layoutPath;
			string fullPath;
			string layoutContent;
			List<string> results = new();

			if (layout == null)
			{
				layoutPath = $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.LAYOUTS_FOLDER}\\{ILayoutManager.DEFAULT_LAYOUT}";
			}
			else
			{
				layoutPath = layout.RelativePath;
			}

			fullPath = System.IO.Path.Combine(this.FolderOptions.Value.GetWebRootFolder(), layoutPath);
			layoutContent = await System.IO.File.ReadAllTextAsync(fullPath);

			System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(layoutContent, "RenderPaneAsync\\(\"(.*)\"\\)");

			foreach (System.Text.RegularExpressions.Match match in matches)
			{
				if (match.Groups.Count == 2)
				{
					results.Add(match.Groups[1].Value);
				}
			}
			
			return results;
		}
	}
}
