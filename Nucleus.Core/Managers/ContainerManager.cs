using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Models;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Nucleus.Core.Managers;

public class ContainerManager : IContainerManager
{
  private IDataProviderFactory DataProviderFactory { get; }

  private const string DEFAULT_CONTAINER = "default.cshtml";

  public ContainerManager(IDataProviderFactory dataProviderFactory)
  {
    this.DataProviderFactory = dataProviderFactory;
  }

  /// <summary>
  /// Retrieve the specified container definition.
  /// </summary>
  /// <returns></returns>
  public async Task<Nucleus.Abstractions.Models.ContainerDefinition> Get(Guid id)
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      return await provider.GetContainerDefinition(id);
    }
  }


  /// <summary>
  /// Return a list of all installed container definitions.
  /// </summary>
  /// <returns></returns>
  public async Task<List<ContainerDefinition>> List()
  {
    using (ILayoutDataProvider provider = this.DataProviderFactory.CreateProvider<ILayoutDataProvider>())
    {
      List<ContainerDefinition> results = await provider.ListContainerDefinitions();
      foreach (ContainerDefinition result in results)
      {
        NormalizeRelativePath(result);
      }
      return results;
    }
  }

  /// <summary>
  /// Replace "\" with "/" in the the RelativePath property of the specified <paramref name="containerDefinition"/> so that path separators are consistent.
  /// </summary>
  /// <param name="containerDefinition"></param>
  /// <returns></returns>
  private ContainerDefinition NormalizeRelativePath(ContainerDefinition containerDefinition)
  {
    if (containerDefinition != null)
    {
      containerDefinition.RelativePath = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(containerDefinition.RelativePath);
    }
    return containerDefinition;
  }

  /// <summary>
  /// Return the container to use for the specified site/page/module.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  /// <param name="containerDefinition"></param>
  /// <returns></returns>
  public string GetEffectiveContainerPath(Site site, Page page, ContainerDefinition containerDefinition)
  {
    return
      containerDefinition?.RelativePath ??
      page.DefaultContainerDefinition?.RelativePath ??
      site.DefaultContainerDefinition?.RelativePath ??
      $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.CONTAINERS_FOLDER}/{DEFAULT_CONTAINER}";
  }

  /// <summary>
  /// Return a list of available styles for the specified container.  The container must support container styles (that is, it must have an 
  /// element with a container-styles="true" attribute), if it does not, no results are returned.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="page"></param>
  /// <param name="containerDefinition"></param>
  /// <returns></returns>
  public Task<List<ContainerStyle>> ListContainerStyles(Site site, Page page, ContainerDefinition containerDefinition)
  {
    List<ContainerStyle> results = new();

    const string CONTAINER_STYLE_PROPERTY_REGEX = "@property\\s*--(?<propertyName>[A-Za-z0-9-]*)\\s*{(\\s*\\/\\*\\s*title:\\s*(?<propertyTitle>[^\\*]*)\\s*\\*\\/)?(\\s*\\/\\*\\s*group:\\s*(?<propertyGroup>[^\\*]*)\\s*\\*\\/)?(\\s*\\/\\*\\s*baseClass:\\s*(?<propertyBaseCssClass>[^\\*]*)\\s*\\*\\/)?(\\s*\\/\\*\\s*preserveOrder: \\s*(?<propertyPreserveOrder>[^\\*]*)\\s*\\*\\/)?\\s*(syntax:\\s*\"(?<propertySyntax>[^\"]*)\"\\s*;)";
    const string PROPERTY_VALUES_REGEX = "-(?<valueName>[A-Za-z0-9]*)\\s*{(\\s*\\/\\*\\s*title:\\s*(?<valueTitle>[^\\*]*))?";

    // container must support container styles, otherwise don't allow any selections
    if (SupportsContainerStyles(site, page, containerDefinition))
    {
      List<string> cssFiles = new List<string>() { "/Shared/Containers/container-styles.css" };
      cssFiles.AddRange(GetContainerCssReferences(site, page, containerDefinition));

      // parse each css file and idenfify @property elements, which are used to define container styles
      foreach (string relativePath in cssFiles)
      {
        string containerCssStylesPath = System.IO.Path.Join(Environment.CurrentDirectory, relativePath);

        if (System.IO.File.Exists(containerCssStylesPath))
        {
          string containerCss = System.IO.File.ReadAllText(containerCssStylesPath);

          // find properties
          foreach (Match stylePropertyMatch in Regex.Matches(containerCss, CONTAINER_STYLE_PROPERTY_REGEX, RegexOptions.Multiline).Cast<Match>())
          {
            string propertyName = stylePropertyMatch.Groups["propertyName"].Value?.Trim();

            ContainerStyle currentStyle = results
              .Where(containerStyle => containerStyle.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
              .FirstOrDefault();

            if (currentStyle == null)
            {
              currentStyle = new(propertyName);
              results.Add(currentStyle);
            }

            currentStyle.Title = stylePropertyMatch.Groups["propertyTitle"].Value?.Trim();
            currentStyle.Group = stylePropertyMatch.Groups["propertyGroup"].Value?.Trim();
            currentStyle.BaseCssClass = stylePropertyMatch.Groups["propertyBaseCssClass"].Value?.Trim() ?? "";
            currentStyle.PreserveOrder = TryParseBoolean(stylePropertyMatch.Groups["propertyPreserveOrder"].Value);
            currentStyle.Syntax = stylePropertyMatch.Groups["propertySyntax"].Value?.Trim() ?? "";

            if (String.IsNullOrEmpty(currentStyle.Title))
            {
              currentStyle.Title = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(currentStyle.Name);
            }
          }

          foreach (ContainerStyle currentStyle in results)
          {
            // find property values
            List<ContainerStyleValue> propertyValues = new();

            foreach (Match stylePropertyValueMatch in Regex.Matches(containerCss, currentStyle.Name + PROPERTY_VALUES_REGEX, RegexOptions.Multiline).Cast<Match>())
            {
              string valueName = stylePropertyValueMatch.Groups["valueName"].Value?.Trim();

              ContainerStyleValue currentStyleValue = currentStyle.Values.Where(value => value.Name == valueName).FirstOrDefault();

              if (currentStyleValue == null)
              {
                currentStyleValue = new() { Name = valueName };
              }

              currentStyleValue.Title = stylePropertyValueMatch.Groups["valueTitle"].Value?.Trim();   
              currentStyleValue.CssClass = $"{currentStyle.Name}-{currentStyleValue.Name}"?.Trim();

              if (String.IsNullOrEmpty(currentStyleValue.Title))
              {
                currentStyleValue.Title = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(currentStyleValue.Name);
              }

              currentStyle.Values.Add(currentStyleValue);
            }
          }
        }
      }

      // sort style values by title, unless preserveValue = true.      
      foreach (ContainerStyle styleValue in results.Where(styleValue => !styleValue.PreserveOrder))
      {
        styleValue.Values = styleValue.Values.OrderBy(value => value.Title).ToList();
      }
    }

    return Task.FromResult(results);
  }

  private Boolean SupportsContainerStyles(Site site, Page page, ContainerDefinition containerDefinition)
  {
    const string SUPPORTS_CONTAINER_STYLES_REGEX = "container-styles\\s*=\\s*\"(?<isSupported>[^\"]*)\"";

    string containerPath = GetEffectiveContainerPath(site, page, containerDefinition);

    if (containerPath != null)
    {
      string containerContent = System.IO.File.ReadAllText(System.IO.Path.Join(Environment.CurrentDirectory, containerPath));

      foreach (Match containerStylesMatch in Regex.Matches(containerContent, SUPPORTS_CONTAINER_STYLES_REGEX, RegexOptions.Multiline).Cast<Match>())
      {
        string value = containerStylesMatch.Groups["isSupported"].Value;
        if (!String.IsNullOrEmpty(value))
        {
          if (Boolean.TryParse(value, out Boolean result))
          {
            return result;
          }
        }
      }
    }

    return false;
  }

  private List<string> GetContainerCssReferences(Site site, Page page, ContainerDefinition containerDefinition)
  {
    List<string> cssFiles = new();

    string cssStylesheetsRegEx = "@Html.AddStyle\\s*\\(\\s*\"(?<cssFile>[^\"]*)";
    string cssStylesheetsRegEx2 = "<link.*href\\s*=\\s*[\"'](?<cssFile>[^\"']*)";

    // parse & read container CSS links.
    // Container css files can extend the available values for container styles which are defined by the "core" container styles css file, and can add
    // new container styles.
    string containerPath = GetEffectiveContainerPath(site, page, containerDefinition);

    if (containerPath != null)
    {
      string containerContent = System.IO.File.ReadAllText(System.IO.Path.Join(Environment.CurrentDirectory, containerPath));
      // look for container stylesheets
      foreach (string regex in new string[] { cssStylesheetsRegEx, cssStylesheetsRegEx2 })
      {
        foreach (Match cssReferenceMatch in Regex.Matches(containerContent, regex, RegexOptions.Multiline).Cast<Match>())
        {
          string cssFile = cssReferenceMatch.Groups["cssFile"].Value?.Trim();
          if (cssFile.StartsWith("~#"))
          {
            // extension folder
            cssFile = System.IO.Path.Join(Nucleus.Core.Plugins.AssemblyLoader.GetExtensionFolderName(System.IO.Path.GetDirectoryName(containerPath)), cssFile[2..]);
          }
          if (cssFile.StartsWith("~!"))
          {
            // folder which contains the view
            cssFile = System.IO.Path.Join(System.IO.Path.GetDirectoryName(containerPath), cssFile[2..]);
          }
          if (cssFile.StartsWith("~"))
          {
            // application folder (just remove the ~, Environment.CurrentDirectory is always used as the base folder)
            cssFile = cssFile[1..];
          }

          cssFiles.Add(cssFile);
        }
      }
    }

    return cssFiles;
  }

  private Boolean TryParseBoolean(string value)
  {
    Boolean result = false;

    if (!String.IsNullOrEmpty(value))
    {
      _ = Boolean.TryParse(value, out result);
    }

    return result;
  }
}
