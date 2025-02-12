﻿using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.FileProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.Web.ViewModels.Admin;

namespace Nucleus.Web.Controllers.Shared;

public class LayoutSelector
{
  public static async Task<ViewModels.Admin.LayoutSelector> BuildLayoutSelectorViewModel(IWebHostEnvironment env, ILayoutManager layoutManager, Guid? selectedLayoutId)
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
      string descriptionFileRelativePath = FindLayoutDescriptionFile(env, basePath);

      layout.Extension = GetExtensionFriendlyName(basePath);
      layout.ThumbnailUrl = FindLayoutThumbnail(env, basePath);

      if (descriptionFileRelativePath != null)
      {
        IFileInfo fileInfo = env.ContentRootFileProvider.GetFileInfo(descriptionFileRelativePath);
        //string descriptionContent = System.IO.File.ReadAllText(System.IO.Path.Join(Environment.CurrentDirectory, descriptionFileRelativePath));
        string descriptionContent = await fileInfo.ReadAllText();

        layout.Description = descriptionContent.ToHtml(GetContentType(System.IO.Path.GetExtension(descriptionFileRelativePath)));
      }
    }

    return viewModel;
  }

  public static async Task<ContainerSelector> BuildContainerSelectorViewModel(IWebHostEnvironment env, IContainerManager containerManager, Guid? selectedContainerId)
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
      string descriptionFileRelativePath = FindContainerDescriptionFile(env, basePath);

      container.Extension = GetExtensionFriendlyName(basePath);
      container.ThumbnailUrl = FindContainerThumbnail(env, basePath);

      if (descriptionFileRelativePath != null)
      {
        IFileInfo fileInfo = env.ContentRootFileProvider.GetFileInfo(descriptionFileRelativePath);
        string descriptionContent = await fileInfo.ReadAllText();// System.IO.File.ReadAllText(System.IO.Path.Join(Environment.CurrentDirectory, descriptionFileRelativePath));
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
    try
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

            string packagePath = System.IO.Path.Join(Environment.CurrentDirectory, Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER, folder, IExtensionManager.PACKAGE_MANIFEST_FILENAME);

            // use package name element if present
            if (System.IO.File.Exists(packagePath))
            {
              System.Xml.Serialization.XmlSerializer serializer = new(typeof(Abstractions.Models.Extensions.Package));
              using (System.IO.Stream stream = System.IO.File.OpenRead(packagePath))
              {
                Abstractions.Models.Extensions.Package package = (Abstractions.Models.Extensions.Package)serializer.Deserialize(stream);
                return package.name;
              }
            }

            // otherwise use folder name: insert spaces in between words, using capital letters to determine word breaks 
            return Regex.Replace(folder, @"([A-Z])([A-Z])([a-z])|([a-z])([A-Z])", "$1$4 $2$3$5");
          }

          workingDirectory = workingDirectoryParent;
        }
      }
    }
    catch  (Exception)
    { 
      // this is a non-critical function, which accesses the file system and does XML deserializaton (things that can fail).  Suppress 
      // errors so that any problems don't stop the layout selector from working
    }

    return "";
  }

  public static string GetContentType(string extension)
  {
    if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase)) return "text/markdown";
    if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) return "text/plain";
    return "text/html";
  }

  public static string FindLayoutThumbnail(IWebHostEnvironment env, string basePath)
  {
    foreach (string fileExtension in new string[] { ".png", ".svg", ".jpg", ".webp" })
    {
      string path = basePath + fileExtension;

      IFileInfo fileInfo = env.ContentRootFileProvider.GetFileInfo(path);
      if (fileInfo.Exists)
      {
        return path;
      }
    }

    return null;
  }

  public static string FindLayoutDescriptionFile(IWebHostEnvironment env, string basePath)
  {
    foreach (string fileExtension in new string[] { ".md", ".htm", ".html", ".txt" })
    {
      string path = basePath + fileExtension;
      
      IFileInfo fileInfo = env.ContentRootFileProvider.GetFileInfo(path);
      if (fileInfo.Exists)
      {
        return path;
      }
    }

    return null;
  }

  public static string FindContainerThumbnail(IWebHostEnvironment env, string basePath) => FindLayoutThumbnail(env, basePath);

  public static string FindContainerDescriptionFile(IWebHostEnvironment env, string basePath) => FindLayoutDescriptionFile(env, basePath);

}
