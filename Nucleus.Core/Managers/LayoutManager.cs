using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders;
using Nucleus.Data.Common;
using Nucleus.Extensions;

namespace Nucleus.Core.Managers;

/// <summary>
/// Provides functions to manage database data for <see cref="LayoutDefinition"/>s.
/// </summary>
public class LayoutManager : ILayoutManager
{
  private IWebHostEnvironment WebHostEnvironment { get; }

  private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

  private IDataProviderFactory DataProviderFactory { get; }

  public LayoutManager(IWebHostEnvironment webHostEnvironment, IDataProviderFactory dataProviderFactory, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
  {
    this.WebHostEnvironment = webHostEnvironment;
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
      return NormalizeRelativePath(await provider.GetLayoutDefinition(id));
    }
  }
  private LayoutDefinition NormalizeRelativePath(LayoutDefinition layoutDefinition)
  {
    if (layoutDefinition != null)
    {
      layoutDefinition.RelativePath = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(layoutDefinition.RelativePath);
    }
    return layoutDefinition;
  }

  /// <summary>
  /// Returns a list of all installed <see cref="LayoutDefinition"/>s.
  /// </summary>
  /// <returns></returns>
  public async Task<IList<LayoutDefinition>> List()
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      IList<LayoutDefinition> results = await provider.ListLayoutDefinitions();
      foreach (LayoutDefinition result in results)
      {
        NormalizeRelativePath(result);
      }
      return results;
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
    //string fullPath;
    List<string> results = new();

    if (layout == null)
    {
      layoutPath = $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.LAYOUTS_FOLDER}/{ILayoutManager.DEFAULT_LAYOUT}";
    }
    else
    {
      layoutPath = layout.RelativePath;
    }

    //fullPath = System.IO.Path.Combine(Nucleus.Abstractions.Models.Configuration.FolderOptions.GetWebRootFolder(), layoutPath);
    await DetectPanes(layoutPath, results);

    return results;
  }

  private async Task DetectPanes(string filePath, List<string> results)
  {
    Microsoft.Extensions.FileProviders.IFileInfo fileInfo = this.WebHostEnvironment.ContentRootFileProvider.GetFileInfo(filePath);
    if (fileInfo.Exists)
    {
      string layoutContent = await fileInfo.ReadAllText(); //System.IO.File.ReadAllTextAsync(filePath);

      // html helper @Html.RenderPaneAsync("pane-name")
      System.Text.RegularExpressions.MatchCollection htmlHelperMatches = System.Text.RegularExpressions.Regex.Matches(layoutContent, "RenderPaneAsync\\(\"(?<panename>[^\\\"]*)\"\\)");

      foreach (System.Text.RegularExpressions.Match match in htmlHelperMatches)
      {
        if (match.Groups.ContainsKey("panename"))
        {
          results.Add(match.Groups["panename"].Value);
        }
      }

      // Tag helper <RenderPane pane-name="name" />
      System.Text.RegularExpressions.MatchCollection tagHelperMatches = System.Text.RegularExpressions.Regex.Matches(layoutContent, "RenderPane[\\s]*name[\\s]*=[\\s]*\\\"(?<panename>[^\\\"]*)\\\"");

      foreach (System.Text.RegularExpressions.Match match in tagHelperMatches)
      {
        if (match.Groups.ContainsKey("panename"))
        {
          results.Add(match.Groups["panename"].Value);
        }
      }

      // html helper PartialAsync
      System.Text.RegularExpressions.MatchCollection htmlHelperReferenceMatches = System.Text.RegularExpressions.Regex.Matches(layoutContent, "PartialAsync\\(\"(?<reference>[^\\\"]*)\"\\)");

      foreach (System.Text.RegularExpressions.Match match in htmlHelperReferenceMatches)
      {
        if (match.Groups.ContainsKey("reference"))
        {
          string referenceFilePath = System.IO.Path.Join(System.IO.Path.GetDirectoryName(filePath), match.Groups["reference"].Value);          
          await DetectPanes(referenceFilePath, results);          
        }
      }

      // tag helper <partial> matches
      System.Text.RegularExpressions.MatchCollection tagHelperReferenceMatches = System.Text.RegularExpressions.Regex.Matches(layoutContent, "partial[\\s]*name[\\s]*=[\\s]*\\\"(?<reference>[^\\\"]*)\\\"");

      foreach (System.Text.RegularExpressions.Match match in tagHelperReferenceMatches)
      {
        if (match.Groups.ContainsKey("reference"))
        {
          string referenceFilePath = System.IO.Path.Join(System.IO.Path.GetDirectoryName(filePath), match.Groups["reference"].Value);          
          await DetectPanes(referenceFilePath, results);          
        }
      }
    }
  }

}
