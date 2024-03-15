using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.Web.ViewModels.Admin;

namespace Nucleus.Web.Controllers.Shared;

public class LayoutSelector
{


  public static async Task<ViewModels.Admin.LayoutSelector> BuildLayoutSelectorViewModel(ILayoutManager layoutManager, Guid? selectedLayoutId)
  {
    ViewModels.Admin.LayoutSelector viewModel = new();

    IList<LayoutDefinition> availableLayouts = await layoutManager.List();

    viewModel.SelectedLayoutId = selectedLayoutId;
    viewModel.Layouts = availableLayouts
      .Select(layout => layout.Copy<ViewModels.Admin.LayoutSelector.LayoutInformation>())
      .ToList();

    foreach (ViewModels.Admin.LayoutSelector.LayoutInformation layout in viewModel.Layouts)
    {
      string basePath = System.IO.Path.Join(System.IO.Path.GetDirectoryName(layout.RelativePath), System.IO.Path.GetFileNameWithoutExtension(layout.RelativePath));
      string descriptionFileRelativePath = FindLayoutDescriptionFile(basePath);

      layout.Extension = GetExtensionFriendlyName(basePath);
      layout.ThumbnailUrl = FindLayoutThumbnail(basePath);

      if (descriptionFileRelativePath != null)
      {
        string descriptionContent = System.IO.File.ReadAllText(System.IO.Path.Join(Environment.CurrentDirectory, descriptionFileRelativePath));
        layout.Description = descriptionContent.ToHtml(GetContentType(System.IO.Path.GetExtension(descriptionFileRelativePath)));
      }
    }

    return viewModel;
  }

  public static async Task<ContainerSelector> BuildContainerSelectorViewModel(IContainerManager containerManager, Guid? selectedContainerId)
  {
    ContainerSelector viewModel = new();

    IList<ContainerDefinition> availableContainers = await containerManager.List();

    viewModel.SelectedContainerId = selectedContainerId;
    viewModel.Containers = availableContainers
      .Select(container => container.Copy<ContainerSelector.ContainerInformation>())
      .ToList();

    foreach (ContainerSelector.ContainerInformation container in viewModel.Containers)
    {
      string basePath = System.IO.Path.Join(System.IO.Path.GetDirectoryName(container.RelativePath), System.IO.Path.GetFileNameWithoutExtension(container.RelativePath));
      string descriptionFileRelativePath = FindContainerDescriptionFile(basePath);

      container.Extension = GetExtensionFriendlyName(basePath);
      container.ThumbnailUrl = FindContainerThumbnail(basePath);

      if (descriptionFileRelativePath != null)
      {
        string descriptionContent = System.IO.File.ReadAllText(System.IO.Path.Join(Environment.CurrentDirectory, descriptionFileRelativePath));
        container.Description = descriptionContent.ToHtml(GetContentType(System.IO.Path.GetExtension(descriptionFileRelativePath)));
      }
    }

    return viewModel;
  }

  /// <summary>
  /// Return the extension folder (the folder immediately following "extensions") from a full path.
  /// </summary>
  /// <param name="fullPath"></param>
  /// <returns></returns>
  private static string GetExtensionFriendlyName(string fullPath)
  {
    if (fullPath.Contains(Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER))
    {
      string workingDirectory = fullPath;
      string workingDirectoryParent;

      while (!String.IsNullOrEmpty(workingDirectory))
      {
        workingDirectoryParent = System.IO.Path.GetDirectoryName(workingDirectory);
        if (System.IO.Path.GetFileName(workingDirectoryParent).Equals(Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER, StringComparison.OrdinalIgnoreCase))
        {
          string folder = System.IO.Path.GetFileName(workingDirectory);
          // insert spaces in between words 
          return Regex.Replace(folder, @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1");
        }

        workingDirectory = workingDirectoryParent;
      }
    }

    return "";
  }

  public static string GetContentType(string extension)
  {
    if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase)) return "text/markdown";
    if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) return "text/plain";
    return "text/html";
  }

  public static string FindLayoutThumbnail(string basePath)
  {
    foreach (string fileExtension in new string[] { ".png", ".svg", ".jpg", ".webp" })
    {
      string path = basePath + fileExtension;
      if (System.IO.File.Exists(System.IO.Path.Join(Environment.CurrentDirectory, path)))
      {
        return path;
      }
    }

    return null;
  }

  public static string FindLayoutDescriptionFile(string basePath)
  {
    foreach (string fileExtension in new string[] { ".md", ".htm", ".html", ".txt" })
    {
      string path = basePath + fileExtension;
      if (System.IO.File.Exists(System.IO.Path.Join(Environment.CurrentDirectory, path)))
      {
        return path;
      }
    }

    return null;
  }

  public static string FindContainerThumbnail(string basePath) => FindLayoutThumbnail(basePath);

  public static string FindContainerDescriptionFile(string basePath) => FindLayoutDescriptionFile(basePath);

}
