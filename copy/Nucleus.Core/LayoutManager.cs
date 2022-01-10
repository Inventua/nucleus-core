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
	/// <summary>
	/// Provides functions to manage database data for <see cref="LayoutDefinition"/>s.
	/// </summary>
	public class LayoutManager
	{
		public const string DEFAULT_LAYOUT = "default.cshtml";

		private DataProviderFactory DataProviderFactory { get; }

		public LayoutManager(DataProviderFactory dataProviderFactory)
		{
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Get the specifed <see cref="LayoutDefinition"/>.
		/// </summary>
		/// <returns></returns>
		public LayoutDefinition Get(Guid id)
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.GetLayoutDefinition(id);
			}
		}

		/// <summary>
		/// Returns a list of all installed <see cref="LayoutDefinition"/>s.
		/// </summary>
		/// <returns></returns>
		public IList<LayoutDefinition> List()
		{
			using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
			{
				return provider.ListLayoutDefinitions();
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
		public IEnumerable<string> ListLayoutPanes(LayoutDefinition layout)
		{
			string layoutPath;
			string fullPath;
			string layoutContent;
			List<string> results = new();

			if (layout == null)
			{
				layoutPath = $"{Folders.LAYOUTS_FOLDER}\\{Nucleus.Core.LayoutManager.DEFAULT_LAYOUT}";
			}
			else
			{
				layoutPath = layout.RelativePath;
			}

			fullPath = System.IO.Path.Combine(Folders.GetWebRootFolder(), layoutPath);
			layoutContent =  System.IO.File.ReadAllText(fullPath);

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
