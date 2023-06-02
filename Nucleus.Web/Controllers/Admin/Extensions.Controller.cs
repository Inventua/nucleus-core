using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace Nucleus.Web.Controllers.Admin
{
  [Area("Admin")]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
  public class ExtensionsController : Controller
  {
    private IExtensionManager ExtensionManager { get; }
    private IHostApplicationLifetime HostApplicationLifetime { get; }
    private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }
    private IOptions<Nucleus.Abstractions.Models.Configuration.StoreOptions> StoreOptions { get; }
    private IExtensionsStoreManager StoreManager { get; }
    private IRestApiClient RestApiClient { get; }

    public ExtensionsController(IHostApplicationLifetime hostApplicationLifetime, IRestApiClient restApiClient, IExtensionsStoreManager storeManager, IExtensionManager extensionManager, IOptions<Nucleus.Abstractions.Models.Configuration.StoreOptions> storeOptions, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
    {
      this.HostApplicationLifetime = hostApplicationLifetime;
      this.RestApiClient = restApiClient;
      this.StoreManager = storeManager;
      this.ExtensionManager = extensionManager;
      this.StoreOptions = storeOptions;
      this.FolderOptions = folderOptions;
    }

    /// <summary>
    /// Display the "extensions" admin page
    /// </summary>
    [HttpGet, HttpPost]
    public async Task<ActionResult> Index(ViewModels.Admin.Extensions viewModel)
    {
      return View("Index", await BuildViewModel(viewModel));
    }

    /// <summary>
		/// 
		/// </summary>
		[HttpGet]
    public ActionResult QueryPackageInstalled(Guid id)
    {
      List<Abstractions.Models.Extensions.package> installedPackages = ListInstalledExtensions();
      Abstractions.Models.Extensions.package installedPackage = installedPackages.Where(package => Guid.Parse(package.id) == id).FirstOrDefault();

      if (installedPackage == null)
      {
        return Json(new { isInstalled = false });
      }
      else
      {
        return Json(new { isInstalled = true, installedPackage.version });
      }
    }

    /// <summary>
		/// 
		/// </summary>
		[HttpPost]
    public async Task<ActionResult> QueryStoreSettings(ViewModels.Admin.Extensions viewModel)
    {
      viewModel.SelectedStore = this.StoreOptions.Value.Stores.Where(store => store.BaseUrl == viewModel.SelectedStoreUrl).FirstOrDefault();

      if (viewModel.SelectedStore == null)
      {
        return BadRequest();
      }

      ExtensionsStoreSettings settings = await this.StoreManager.Get(viewModel.SelectedStore.BaseUrl);

      if (settings == null || settings.StoreId == Guid.Empty)
      {
        return Json(new { allowInstall = false });
      }
      else
      {
        return Json(new { allowInstall = true });
      }
    }

    /// <summary>
		/// 
		/// </summary>
		[HttpPost]
    public async Task<ActionResult> Register(ViewModels.Admin.Extensions viewModel)
    {
      ExtensionsStoreSettings settings;

      viewModel.SelectedStore = this.StoreOptions.Value.Stores.Where(store => store.BaseUrl == viewModel.SelectedStoreUrl).FirstOrDefault();
      
      if (viewModel.SelectedStore == null)
      {
        viewModel.SelectedStore = this.StoreOptions.Value.Stores.FirstOrDefault();
      }

      if (viewModel.SelectedStore == null)
      {
        return BadRequest("No store available.");
      }

      settings = await this.StoreManager.Get(viewModel.SelectedStore.BaseUrl);

      try
      {
        SubscriberResponse response;

        if (settings == null || settings.StoreId == Guid.Empty)
        {
          // call the store to generate a new subscriber id
          response = await this.RestApiClient.GetResponse<SubscriberResponse>
          (
            viewModel.SelectedStore.BaseUrl, 
            viewModel.SelectedStore.APIPath, 
            HttpMethod.Put, 
            "create-subscriber", 
            new 
            {
              name = viewModel.SubscriptionName,
              username = viewModel.Username,
              password = viewModel.Password
            });
        }
        else
        {
          // call the store to update an existing subscriber id
          response = await this.RestApiClient.GetResponse<SubscriberResponse>
          (
            viewModel.SelectedStore.BaseUrl, 
            viewModel.SelectedStore.APIPath, 
            HttpMethod.Post, 
            "update-subscriber", 
            new 
            { 
              Id = settings.StoreId, 
              name = viewModel.SubscriptionName, 
              username = viewModel.Username, 
              password = viewModel.Password
            }
          );
        }

        if (settings == null)
        {
          settings = await this.StoreManager.CreateNew(viewModel.SelectedStore.BaseUrl, User);
        }

        settings.StoreId = response.Id;
        await this.StoreManager.Save(settings);
        
        return await Index(viewModel);
      }
      catch (HttpRequestException ex)
      {
        return BadRequest(ex.Message);
      }
    }

    private class SubscriberResponse
    {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public Boolean IsOwnerAssigned { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> InstallPackage(ViewModels.Admin.Extensions viewModel, Guid id)
    {
      // Make sure selected Url is valid (is present in config)      
      viewModel.SelectedStore = this.StoreOptions.Value.Stores.Where(store => store.BaseUrl == viewModel.SelectedStoreUrl).FirstOrDefault();

      if (viewModel.SelectedStore == null)
      {
        return BadRequest();
      }

      try
      {
        using (Stream package = await this.RestApiClient.GetResponseAsStream(viewModel.SelectedStore.BaseUrl, viewModel.SelectedStore.APIPath, HttpMethod.Get, $"get-package", new { id }))
        {
          Abstractions.Models.Extensions.PackageResult result = await this.ExtensionManager.ValidatePackage(package);

          if (result.IsValid)
          {
            viewModel.FileId = result.FileId;
            viewModel.Package = result.Package;
            viewModel.Readme = result.Readme;
            viewModel.License = result.License;
          }
          else
          {
            SetMessages(viewModel, result.Messages);
            return View("Complete", viewModel);
          }
        }

        return View("Wizard", viewModel);

      }
      catch (HttpRequestException ex)
      {
        return BadRequest(ex.Message);
      }
    }

    [HttpPost]
    // This (RequestSizeLimit attribute) doesn't work & generates a warning: "A request body size limit could not be applied. The IHttpRequestBodySizeFeature for the server is read-only."
    // because the FileIntegrityCheckerMiddleware reads the file in order to check a file signature, which (indirectly) sets 
    // IHttpMaxRequestBodySizeFeature.IsReadOnly to true.  Code to handle this route as a special case has been added to 
    // the core FileIntegrityCheckerMiddleware class.
    // [RequestSizeLimit(67108864)] 
    public async Task<ActionResult> Upload([FromForm] IFormFile extensionFile)
    {
      ViewModels.Admin.Extensions viewModel = new();

      if (ControllerContext.ModelState.IsValid)
      {
        if (extensionFile != null)
        {
          if (System.IO.Path.GetExtension(extensionFile.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
          {
            try
            {
              using (Stream package = extensionFile.OpenReadStream())
              {
                Abstractions.Models.Extensions.PackageResult result = await this.ExtensionManager.ValidatePackage(package);

                if (result.IsValid)
                {
                  viewModel.FileId = result.FileId;
                  viewModel.Package = result.Package;
                  viewModel.Readme = result.Readme;
                  viewModel.License = result.License;
                }
                else
                {
                  SetMessages(viewModel, result.Messages);
                  return View("Complete", viewModel);
                }
              }
            }
            catch (Exception ex)
            {
              return BadRequest(ex.Message);
            }
          }
          else
          {
            return BadRequest("File does not have a valid extension.");
          }

        }
        else
        {
          return BadRequest("No file was received.");
        }
      }
      else
      {
        // When there is a problem receiving the uploaded file, the error is in ModelState
        SetMessages(viewModel, ControllerContext.ModelState);
        return View("Complete", viewModel);
      }

      return View("Wizard", viewModel);
    }

    private static void SetMessages(ViewModels.Admin.Extensions viewModel, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
      if (!modelState.IsValid)
      {
        foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry entry in modelState.Values)
        {
          foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in entry.Errors)
          {
            viewModel.Messages.Add(error.ErrorMessage);
          }
        }
      }
    }

    [HttpPost]
    public async Task<ActionResult> Install(ViewModels.Admin.Extensions viewModel)
    {
      if (String.IsNullOrEmpty(viewModel.FileId))
      {
        return BadRequest();
      }
      else
      {
        try
        {
          await this.ExtensionManager.InstallExtension(viewModel.FileId);
          ExtensionManager.DeleteTempFile(viewModel.FileId);
        }
        catch (Exception ex)
        {
          return BadRequest(ex.Message);
        }
      }

      this.HostApplicationLifetime.StopApplication();

      return View("Complete", viewModel);
    }

    [HttpPost]
    public async Task<ActionResult> Uninstall(ViewModels.Admin.Extensions viewModel, Guid id)
    {
      Abstractions.Models.Extensions.package uninstallPackage = null;

      // find the package manifest with the specified id
      foreach (string extensionFolder in System.IO.Directory.GetDirectories(this.FolderOptions.Value.GetExtensionsFolder()))
      {
        string extensionPackageFilename = System.IO.Path.Combine(extensionFolder, IExtensionManager.PACKAGE_MANIFEST_FILENAME);
        Abstractions.Models.Extensions.package package;

        if (System.IO.File.Exists(extensionPackageFilename))
        {
          System.Xml.Serialization.XmlSerializer serializer = new(typeof(Abstractions.Models.Extensions.package));
          using (System.IO.Stream stream = System.IO.File.OpenRead(extensionPackageFilename))
          {
            package = (Abstractions.Models.Extensions.package)serializer.Deserialize(stream);
          }

          if (Guid.Parse(package.id) == id)
          {
            uninstallPackage = package;
            break;
          }
        }
      }

      if (uninstallPackage == null)
      {
        return BadRequest();
      }

      try
      {
        this.ExtensionManager.UninstallExtension(uninstallPackage);
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }

      return View("Complete", await BuildViewModel(viewModel));
    }

    private async Task<ViewModels.Admin.Extensions> BuildViewModel(ViewModels.Admin.Extensions viewModel)
    {
      viewModel.InstalledExtensions = new();
      viewModel.StoreOptions = this.StoreOptions.Value;

      viewModel.SelectedStore = this.StoreOptions.Value.Stores.Where(store => store.BaseUrl == viewModel.SelectedStoreUrl).FirstOrDefault();

      if (viewModel.SelectedStore == null)
      {
        viewModel.SelectedStore = viewModel.StoreOptions.Stores.FirstOrDefault();
      }

      viewModel.InstalledExtensions = ListInstalledExtensions();
      viewModel.StoreSettings = await this.StoreManager.Get(viewModel.SelectedStore.BaseUrl);

      if (viewModel.StoreSettings != null && viewModel.StoreSettings.StoreId != Guid.Empty)
      {
        try
        { 
          SubscriberResponse response = await this.RestApiClient.GetResponse<SubscriberResponse>(viewModel.SelectedStore.BaseUrl, viewModel.SelectedStore.APIPath, HttpMethod.Get, "get-subscriber", new { Id = viewModel.StoreSettings.StoreId });
          if (response == null)
          {
            // store Id is invalid
            viewModel.StoreSettings.StoreId = Guid.Empty;
          }
          else
          {
            viewModel.SubscriptionName = response.Name;
            viewModel.IsOwnerAssigned = response.IsOwnerAssigned;
          }
        }
        catch (HttpRequestException)
        {
          // suppress exception.  The call to get-subscriber is used to display messages prompting users to register, the extensions store works without
          // registering, so this is a helpful but not essential function.
        }
      }

      System.Uri uri = new (new System.Uri(viewModel.SelectedStore.BaseUrl), viewModel.SelectedStore.ViewerPath);

      viewModel.StoreUrl= new Uri(uri, $"?version={typeof(ExtensionsController).Assembly.GetName().Version}&track={viewModel.StoreSettings?.Track}").ToString();

      return viewModel;
    }

    private List<Abstractions.Models.Extensions.package> ListInstalledExtensions()
    {
      List<Abstractions.Models.Extensions.package> results = new();
      foreach (string extensionFolder in System.IO.Directory.GetDirectories(this.FolderOptions.Value.GetExtensionsFolder()))
      {
        string extensionPackageFilename = System.IO.Path.Combine(extensionFolder, IExtensionManager.PACKAGE_MANIFEST_FILENAME);
        Abstractions.Models.Extensions.package package;

        if (System.IO.File.Exists(extensionPackageFilename))
        {
          System.Xml.Serialization.XmlSerializer serializer = new(typeof(Abstractions.Models.Extensions.package));
          using (System.IO.Stream stream = System.IO.File.OpenRead(extensionPackageFilename))
          {
            package = (Abstractions.Models.Extensions.package)serializer.Deserialize(stream);
          }

          results.Add(package);
        }
      }

      return results;
    }
  }
}
