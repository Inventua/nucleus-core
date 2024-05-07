using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Permissions;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.ViewFeatures;

namespace Nucleus.Web.Controllers.Admin
{
  [Area("Admin")]
  public class PagesController : Controller
  {
    private IWebHostEnvironment WebHostEnvironment { get; }
    private ILogger<PagesController> Logger { get; }
    private Context Context { get; }
    private ILayoutManager LayoutManager { get; }
    private IContainerManager ContainerManager { get; }
    private IPageManager PageManager { get; }
    private IPageModuleManager PageModuleManager { get; }
    private IRoleManager RoleManager { get; }
    private IFileSystemManager FileSystemManager { get; set; }
    private IUserManager UserManager { get; set; }
    private IContentManager ContentManager { get; set; }

    public const string CUSTOM_CONTAINER_STYLE_VALUE = "__custom";

    public PagesController(
      IWebHostEnvironment webHostEnvironment,
      Context context,
      ILogger<PagesController> logger,
      IPageManager pageManager,
      IPageModuleManager pageModuleManager,
      IFileSystemManager fileSystemManager,
      IRoleManager roleManager,
      IUserManager userManager,
      ILayoutManager layoutManager,
      IContentManager contentManager,
      IContainerManager containerManager)
    {
      this.WebHostEnvironment = webHostEnvironment;
      this.Context = context;
      this.Logger = logger;
      this.PageManager = pageManager;
      this.PageModuleManager = pageModuleManager;
      this.UserManager = userManager;
      this.RoleManager = roleManager;
      this.LayoutManager = layoutManager;
      this.ContainerManager = containerManager;
      this.ContentManager = contentManager;
      this.FileSystemManager = fileSystemManager;
    }

    /// <summary>
    /// Display the pages index
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    public async Task<ActionResult> Index()
    {
      return View("Index", await BuildIndexViewModel());
    }

    /// <summary>
    /// Search pages and display the results
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    public async Task<ActionResult> Search(ViewModels.Admin.PageIndex viewModel)
    {
      User user = await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId());

      List<Role> roles = new() { this.Context.Site.AllUsersRole };

      if (HttpContext.User.IsSiteAdmin(this.Context.Site))
      {
        roles = null;
      }
      else if (HttpContext.User.IsAnonymous())
      {
        roles.Add(this.Context.Site.AnonymousUsersRole);
      }
      else
      {
        roles.AddRange(user?.Roles);
      }

      viewModel.SearchResults = await this.PageManager.Search(this.Context.Site, viewModel.SearchTerm, roles, viewModel.SearchResults);

      return View("SearchResults", viewModel);
    }

    /// <summary>
    /// Display the page editor
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> Editor(Guid id, ViewModels.Admin.PageEditor.PageEditorModes mode)
    {
      ViewModels.Admin.PageEditor viewModel;
      Page page;

      if (id == Guid.Empty)
      {
        page = await this.PageManager.CreateNew(this.Context.Site);
      }
      else
      {
        page = await this.PageManager.Get(id);
      }

      viewModel = await BuildPageEditorViewModel(page, null, true);

      viewModel.PageEditorMode = mode;


      return View("Editor", viewModel);
    }

    /// <summary>
    /// Display the layout selector
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> LayoutSelector(ViewModels.Admin.PageEditor viewModel)
    {
      Guid? selectedLayoutId = viewModel.Page.LayoutDefinition?.Id ?? this.Context.Site.DefaultLayoutDefinition?.Id;
      if (selectedLayoutId == null || selectedLayoutId == Guid.Empty)
      {
        selectedLayoutId = Guid.Parse("2FF6818A-09FE-4EE2-BEFF-495A876AB6D6");
      }
      return View("_LayoutSelector", await Shared.LayoutSelector.BuildLayoutSelectorViewModel(this.WebHostEnvironment, this.LayoutManager, selectedLayoutId));
    }

    /// <summary>
    /// Display the container selector
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> ContainerSelector(ViewModels.Admin.PageEditor viewModel)
    {
      Guid? selectedContainerId = viewModel.Page.DefaultContainerDefinition?.Id ?? this.Context.Site.DefaultLayoutDefinition?.Id;
      if (selectedContainerId == null || selectedContainerId == Guid.Empty)
      {
        selectedContainerId = Guid.Parse("80A7F079-F61D-42A4-9A4B-DA7692415952");
      }
      return View("_ContainerSelector", await Shared.LayoutSelector.BuildContainerSelectorViewModel(this.WebHostEnvironment, this.ContainerManager, selectedContainerId));
    }

    /// <summary>
    /// Display the container selector
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> ModuleContainerSelector(ViewModels.Admin.PageModuleCommonSettingsEditor viewModel)
    {
      Page page = await this.PageManager.Get(viewModel.Module.PageId);

      Guid? selectedContainerId = viewModel.Module.ContainerDefinition?.Id ?? page?.DefaultContainerDefinition?.Id ?? this.Context.Site.DefaultLayoutDefinition?.Id;
      if (selectedContainerId == null || selectedContainerId == Guid.Empty)
      {
        selectedContainerId = Guid.Parse("80A7F079-F61D-42A4-9A4B-DA7692415952");
      }
      return View("_ContainerSelector", await Shared.LayoutSelector.BuildContainerSelectorViewModel(this.WebHostEnvironment, this.ContainerManager, selectedContainerId));
    }

    /// <summary>
    /// Display the page editor
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> NewPageBlank(ViewModels.Admin.PageEditor.PageEditorModes mode)
    {
      ViewModels.Admin.PageEditor viewModel;
      Page page = await this.PageManager.CreateNew(this.Context.Site);

      viewModel = await BuildPageEditorViewModel(page, null, true);

      viewModel.PageEditorMode = mode;

      return View("Editor", viewModel);
    }

    /// <summary>
		/// Display the page editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> NewPageFromCopy(Guid sourcePageId)
    {
      ViewModels.Admin.PageIndex viewModel;

      // use specified page as a template
      Page sourcePage = await this.PageManager.Get(sourcePageId);
      if (sourcePage == null) return BadRequest($"Invalid {nameof(sourcePageId)}.");

      Page page = sourcePage.Copy<Page>();

      // reset ids & other fields
      page.Id = Guid.Empty;
      page.Name = $"Copy of {page.Name}";
      page.Title = "";

      // new page can't use the template page's routes
      page.Routes.Clear();

      foreach (Permission permission in page.Permissions)
      {
        permission.Id = Guid.NewGuid();
      }
      foreach (PageModule module in page.Modules)
      {
        module.Id = Guid.NewGuid();
      }
      foreach (Permission permission in page.Modules.SelectMany(module => module.Permissions))
      {
        permission.Id = Guid.NewGuid();
      }

      // save the new page and re-load it.  We must save/load so that modules are available in the new page (copy)
      await this.PageManager.Save(this.Context.Site, page);
      foreach (PageModule module in page.Modules)
      {
        await this.PageModuleManager.Save(page, module);
      }

      viewModel = await BuildIndexViewModel(page.Id, true);

      return View("Index", viewModel);
    }

    /// <summary>
    /// Display the page editor
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> NewPageFromTemplate(Guid templateId)
    {
      ViewModels.Admin.PageIndex viewModel;

      // use specified template file 
      Nucleus.Abstractions.Models.FileSystem.File templateFile = await this.FileSystemManager.GetFile(this.Context.Site, templateId);
      if (templateFile == null) return BadRequest($"Invalid {nameof(templateId)}.");

      Nucleus.Abstractions.Models.Export.PageTemplate template;

      using (System.IO.Stream stream = await this.FileSystemManager.GetFileContents(this.Context.Site, templateFile))
      {
        template = await this.PageManager.ParseTemplate(stream);
      }

      Page page = template.Page;

      // no need to reset ids, because ParseTemplate auto-generates new Ids, but we do need to clear some fields
      page.Name = "new page";
      page.Title = "";

      // new page can't use the template page's routes
      page.Routes.Clear();
      page.DefaultPageRouteId = null;

      // page templates don't have permissions

      // validate the layout and container selection, set blank if they have not been installed
      IList<LayoutDefinition> availableLayouts = await this.LayoutManager.List();
      if (!availableLayouts.Where(available => available.Id == page.LayoutDefinition?.Id).Any())
      {
        page.LayoutDefinition = null;
      }

      List<ContainerDefinition> availableContainers = await this.ContainerManager.List();
      if (!availableContainers.Where(available => available.Id == page.DefaultContainerDefinition?.Id).Any())
      {
        page.DefaultContainerDefinition = null;
      }

      IEnumerable<ModuleDefinition> availableModules = await this.PageModuleManager.ListModuleDefinitions();
      foreach (PageModule module in page.Modules)
      {
        // validate that the module is installed, fail if missing
        if (!availableModules.Where(available => available.Id == module.ModuleDefinition.Id).Any())
        {
          return this.PopupMessage("New Page - Template", $"The '{System.IO.Path.GetFileNameWithoutExtension(templateFile.Name)}' template contains an instance of the '{module.ModuleDefinition.FriendlyName}' module, which is not installed. You will need to install the module before you can use this template.", ControllerExtensions.PopupIcons.Warning);
        }

        // validate container selection for the module, set blank if not installed
        if (!availableContainers.Where(available => available.Id == module.ContainerDefinition?.Id).Any())
        {
          module.ContainerDefinition = null;
        }
      }

      // save the new page and re-load it.  We must save/load so that modules are available in the new page (copy)
      await this.PageManager.Save(this.Context.Site, page);
      foreach (PageModule module in page.Modules)
      {
        await this.PageModuleManager.Save(page, module);

        // if there are any content elements which relate to the module save them.  
        foreach (Content content in template.Contents.Where(content => content.PageModuleId == module.Id))
        {
          await this.ContentManager.Save(module, content);
        }
      }


      page = await this.PageManager.Get(page.Id);

      viewModel = await BuildIndexViewModel(page.Id, true);

      return View("Index", viewModel);
    }

    /// <summary>
		/// Display the page editor
		/// </summary>
		/// <returns></returns>
		[HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> Select(ViewModels.Admin.PageEditor viewModel)
    {
      Nucleus.Abstractions.Models.FileSystem.File selectedLinkFile = null;

      if (viewModel.SelectedLinkFile != null)
      {
        selectedLinkFile = await this.FileSystemManager.RefreshProperties(this.Context.Site, viewModel.SelectedLinkFile);
      }

      viewModel = await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true);

      if (selectedLinkFile != null)
      {
        viewModel.SelectedLinkFile = selectedLinkFile;
      }

      return View("Editor", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> SelectAnotherLinkFile(ViewModels.Admin.PageEditor viewModel)
    {
      viewModel.SelectedLinkFile.ClearSelection();
      return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true));
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> DeletePage(ViewModels.Admin.PageEditor viewModel)
    {
      await this.PageManager.Delete(viewModel.Page);

      return View("Index", await BuildIndexViewModel());
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> AddPageRoute(ViewModels.Admin.PageEditor viewModel)
    {
      viewModel.Page.Routes.Add(new PageRoute());

      return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true));
    }

    /// <summary>
    /// Remove the route specified by id from the currently selected page's routes.  
    /// </summary>
    /// <param name="viewModel"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <remarks>
    /// If a route with the specified id is not present in the currently selected page's routes, no action is taken and no exception is 
    /// generated.  The "delete" occurs within the viewModel only - the Save action must be called in order to commit the delete operation
    /// to the database.
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> RemovePageRoute(ViewModels.Admin.PageEditor viewModel, Guid id)
    {

      foreach (PageRoute route in viewModel.Page.Routes)
      {
        if (route.Id == id)
        {
          viewModel.Page.Routes.Remove(route);
          break;
        }
      }

      return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, true));
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> RemovePagePermissionRole(ViewModels.Admin.PageEditor viewModel, Guid id)
    {
      viewModel.Page.Permissions = await RebuildPermissions(viewModel.PagePermissions);

      foreach (Permission permission in viewModel.Page.Permissions.ToList())
      {
        if (permission.Role.Id == id)
        {
          viewModel.Page.Permissions.Remove(permission);
        }
      }

      viewModel.PagePermissions = viewModel.Page.Permissions.ToPermissionsList(this.Context.Site);
      viewModel.DisableCopy = true;

      return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, false));
    }

    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> CreateTemplateAndDownload(Guid pageId)
    {
      Page page = await this.PageManager.Get(pageId);

      if (page == null) return BadRequest("Invalid page Id.");

      string filename = $"{page.Name}.xml";
      System.IO.Stream export = await this.PageManager.Export(page);

      // download
      return File(export, "text/xml", filename);
    }


    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> CreateTemplateAndSave(Guid pageId)
    {
      Page page = await this.PageManager.Get(pageId);

      if (page == null) return BadRequest("Invalid page Id.");

      string filename = $"{page.Name}.xml";
      System.IO.Stream export = await this.PageManager.Export(page);

      // save
      await SaveTemplate(export, filename);
      return this.PopupMessage("Save Template", "Page template saved.", ControllerExtensions.PopupIcons.Info);
      //return Json(new { Title = "Save Template", Message = "Page template saved.", Icon = "info" });

    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> Save(ViewModels.Admin.PageEditor viewModel)
    {
      Boolean isNew = (viewModel.Page.Id == Guid.Empty);

      // prevent validation errors when a link file is not selected
      ControllerContext.ModelState.Remove<ViewModels.Admin.PageEditor>(viewModel => viewModel.SelectedLinkFile.Id);

      if (!ControllerContext.ModelState.IsValid)
      {
        return BadRequest(ControllerContext.ModelState);
      }

      viewModel.Page.Permissions = await RebuildPermissions(viewModel.PagePermissions);
      viewModel.Page.LinkFileId = viewModel.SelectedLinkFile?.Id;

      await this.PageManager.Save(this.Context.Site, viewModel.Page);

      if (viewModel.PageEditorMode != ViewModels.Admin.PageEditor.PageEditorModes.Default)
      {
        if (viewModel.Page.DefaultPageRoute() != null)
        {
          string location = Url.GetAbsoluteUri(viewModel.Page.DefaultPageRoute().Path).ToString();
          //ControllerContext.HttpContext.Response.Headers.Add("X-Location", location);
          ControllerContext.HttpContext.Response.Headers.Append("X-Location-Target", "_top");
          //return StatusCode((int)System.Net.HttpStatusCode.Found);
          return ControllerContext.HttpContext.NucleusRedirect(location);
        }
        else
        {
          return Ok();
        }
      }
      else
      {
        return View("Index", await BuildIndexViewModel(viewModel.Page.Id, isNew));
      }
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    public async Task<ActionResult> CopyPermissionsReplaceAll(ViewModels.Admin.PageEditor viewModel)
    {
      if (await this.PageManager.CopyPermissionsToDescendants(this.Context.Site, viewModel.Page, User, CopyPermissionOperation.Replace))
      {
        return Json(new { Title = "Copy Permissions to Descendants", Message = "Permissions were copied successfully.", Icon = "alert" });
      }
      else
      {
        return BadRequest();
      }
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    public async Task<ActionResult> CopyPermissionsMerge(ViewModels.Admin.PageEditor viewModel)
    {
      if (await this.PageManager.CopyPermissionsToDescendants(this.Context.Site, viewModel.Page, User, CopyPermissionOperation.Merge))
      {
        return Json(new { Title = "Copy Permissions to Descendants", Message = "Permissions were copied successfully.", Icon = "alert" });
      }
      else
      {
        return BadRequest();
      }
    }


    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    public async Task<ActionResult> MovePageDown(ViewModels.Admin.PageEditor viewModel, Guid id)
    {
      await this.PageManager.MoveDown(this.Context.Site, id);

      return View("Index", await BuildIndexViewModel(id, true));
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
    public async Task<ActionResult> MovePageUp(ViewModels.Admin.PageEditor viewModel, Guid id)
    {
      await this.PageManager.MoveUp(this.Context.Site, id);

      return View("Index", await BuildIndexViewModel(id, true));
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> MoveModuleTo(Guid? beforeModuleId, string pane)
    {
      PageModule beforeModule = null;

      if (beforeModuleId.HasValue && beforeModuleId.Value != Guid.Empty)
      {
        beforeModule = await this.PageModuleManager.Get(beforeModuleId.Value);
      }

      await this.PageModuleManager.MoveTo(this.Context.Page, this.Context.Module, pane, beforeModule);
      return Ok();
    }


    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> UpdateModuleTitle(string value)
    {
      this.Context.Module.Title = value;
      await this.PageModuleManager.Save(this.Context.Page, this.Context.Module);
      return Ok();
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> RemoveModulePermissionRole(ViewModels.Admin.PageModuleCommonSettingsEditor viewModel, Guid id)
    {
      viewModel.Module.Permissions = await RebuildPermissions(viewModel.ModulePermissions);

      // .ToList is to make a copy so we don't get an error when we .Remove
      foreach (Permission permission in viewModel.Module.Permissions.ToList())  
      {
        if (permission.Role.Id == id)
        {
          viewModel.Module.Permissions.Remove(permission);
        }
      }

      Page page = await this.PageManager.Get(viewModel.Module.PageId);
      viewModel.ModulePermissions = viewModel.Module.Permissions.ToPermissionsList(this.Context.Site);

      return View("ModuleCommonSettings", await BuildPageModuleCommonSettingsViewModel(page, viewModel.Module, viewModel.ModulePermissions, false));
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> AddModule(ViewModels.Admin.PageEditor viewModel)
    {
      if (viewModel.Page.Id == Guid.Empty)
      {
        return BadRequest("You must save your new page before adding a module.");
      }

      Page page = await this.PageManager.Get(viewModel.Page.Id);
      PageModule module = await this.PageModuleManager.CreateNew(this.Context.Site, page);

      return View("ModuleCommonSettings", await BuildPageModuleCommonSettingsViewModel(page, module, null, true));
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> EditModuleCommonSettings(ViewModels.Admin.PageEditor viewModel, Guid mid, ViewModels.Admin.PageEditor.PageEditorModes mode)
    {
      Page page = await this.PageManager.Get(viewModel.Page.Id);
      PageModule module = await this.PageModuleManager.Get(mid);

      ViewModels.Admin.PageModuleCommonSettingsEditor outputViewModel = await BuildPageModuleCommonSettingsViewModel(page, module, null, true);
      outputViewModel.PageEditorMode = mode;

      if (mode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)
      {
        outputViewModel.UseLayout = "_PopupEditor";
      }

      return View("ModuleCommonSettings", outputViewModel);
    }

    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> EditModuleCommonSettings(Guid mid, ViewModels.Admin.PageEditor.PageEditorModes mode)
    {
      ViewModels.Admin.PageModuleCommonSettingsEditor viewModel;

      PageModule module = await this.PageModuleManager.Get(mid);
      Page page = await this.PageManager.Get(module.PageId);

      viewModel = await BuildPageModuleCommonSettingsViewModel(page, module, null, true);
      viewModel.PageEditorMode = mode;

      if (mode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)
      {
        viewModel.UseLayout = "_PopupEditor";
      }

      return View("ModuleCommonSettings", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> RefreshContainerStyles(ViewModels.Admin.PageModuleCommonSettingsEditor viewModel)
    {
      Page page = await this.PageManager.Get(viewModel.Module.PageId);
      viewModel.Module.ContainerDefinition = await this.ContainerManager.Get(viewModel.Module.ContainerDefinition.Id);

      viewModel.Module.AutomaticClasses = BuildAutomaticClasses(viewModel.ModuleContainerStyles);
      viewModel.Module.AutomaticStyles = BuildAutomaticStyles(viewModel.ModuleContainerStyles);

      // if the Automatic classes/styles are empty, re-read the values from the original record
      if (String.IsNullOrEmpty(viewModel.Module.AutomaticClasses) || String.IsNullOrEmpty(viewModel.Module.AutomaticStyles))
      { 
        PageModule original = await this.PageModuleManager.Get(viewModel.Module.Id);

        if (original != null)
        {
          if (String.IsNullOrEmpty(viewModel.Module.AutomaticClasses))
          {
            viewModel.Module.AutomaticClasses = original.AutomaticClasses;
          }
          
          if (String.IsNullOrEmpty(viewModel.Module.AutomaticStyles))
          {
            viewModel.Module.AutomaticStyles = original.AutomaticStyles;
          }
        }
      }

      return View("_ContainerStyles", await BuildPageModuleCommonSettingsViewModel(page, viewModel.Module, null, false));
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> SaveModuleCommonSettings(ViewModels.Admin.PageModuleCommonSettingsEditor viewModel)
    {
      if (viewModel.Module.ModuleDefinition == null || viewModel.Module.ModuleDefinition.Id == Guid.Empty)
      {
        ModelState.Clear();
        ModelState.AddModelError<ViewModels.Admin.PageModuleCommonSettingsEditor>(model => model.Module.ModuleDefinition.Id, "Please select a module type.");
        return BadRequest(ModelState);
      }

      Page page = await this.PageManager.Get(viewModel.Module.PageId);
      viewModel.Module.Permissions = await RebuildPermissions(viewModel.ModulePermissions);

      viewModel.Module.AutomaticClasses = BuildAutomaticClasses(viewModel.ModuleContainerStyles);
      viewModel.Module.AutomaticStyles= BuildAutomaticStyles(viewModel.ModuleContainerStyles);

      await this.PageModuleManager.Save(page, viewModel.Module);

      if (viewModel.PageEditorMode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)
      {
        return Ok();
      }
      else
      {
        // we need to reload the page so that the new module or module changes are present in the modules list
        page = await this.PageManager.Get(viewModel.Module.PageId);
        return View("_PageModules", await BuildPageEditorViewModel(page, null, false));
      }
    }

    private string BuildAutomaticClasses(List<ContainerStyle> values)
    {
      if (values == null) return "";

      string[] cssClasses = values
        .SelectMany(style => new string[] { !String.IsNullOrEmpty(style.SelectedValue) || !String.IsNullOrEmpty(style.CustomValue) ? style.BaseCssClass : null, style.SelectedValue })
        .Where(cssClass => !String.IsNullOrEmpty(cssClass) && cssClass != CUSTOM_CONTAINER_STYLE_VALUE)
        .Distinct()
        .ToArray();

      return String.Join(' ', cssClasses);
    }

    private string BuildAutomaticStyles(List<ContainerStyle> values)
    {
      if (values == null) return "";

      string[] cssStyles = values
        .Where(style => style.SelectedValue == CUSTOM_CONTAINER_STYLE_VALUE)
        .Select(style => $"--{style.Name}:{style.CustomValue}")
        .Distinct()
        .ToArray();

      return String.Join(' ', cssStyles);
    }

    [HttpPost]
    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> EditModule(Guid mid, ViewModels.Admin.PageEditor.PageEditorModes mode)
    {
      ViewModels.Admin.PageModuleEditor viewModel = await BuildPageModuleViewModel(mid);


      if (mode == ViewModels.Admin.PageEditor.PageEditorModes.Standalone)
      {
        viewModel.UseLayout = "_PopupEditor";
      }

      return View("ModuleEditor", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> EditModulePermissions(Guid mid)
    {
      ViewModels.Admin.PageModuleCommonSettingsEditor viewModel;

      PageModule module = await this.PageModuleManager.Get(mid);
      module.Permissions = await this.PageModuleManager.ListPermissions(module);
      viewModel = await BuildPageModuleCommonSettingsViewModel(await this.PageManager.Get(module), module, null, true);

      return View("ModulePermissionsEditor", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> AddModulePermissionRole(ViewModels.Admin.PageModuleCommonSettingsEditor viewModel, [FromQuery] Boolean Standalone)
    {
      if (viewModel.SelectedModuleRoleId != Guid.Empty)
      {
        viewModel.Module.Permissions = await RebuildPermissions(viewModel.ModulePermissions);
        await this.PageModuleManager.CreatePermissions(this.Context.Site, viewModel.Module, await this.RoleManager.Get(viewModel.SelectedModuleRoleId));
      }

      Page page = await this.PageManager.Get(viewModel.Module.PageId);
      viewModel.ModulePermissions = viewModel.Module.Permissions.ToPermissionsList(this.Context.Site);

      viewModel = await BuildPageModuleCommonSettingsViewModel(page, viewModel.Module, viewModel.ModulePermissions, false);

      if (Standalone)
      {
        return View("ModulePermissionsEditor", viewModel);
      }
      else
      {
        return View("ModuleCommonSettings", viewModel);
      }
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
    public async Task<ActionResult> SaveModulePermissions(ViewModels.Admin.PageModuleCommonSettingsEditor viewModel, Guid mid)
    {
      Page page = await this.PageManager.Get(viewModel.Module.PageId);
      PageModule module = await this.PageModuleManager.Get(mid);      
      module.Permissions = await RebuildPermissions(viewModel.ModulePermissions);

      await this.PageModuleManager.SavePermissions(page, module);

      return Ok();
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> DeleteModule(ViewModels.Admin.PageEditor viewModel, Guid mid)
    {
      await this.PageModuleManager.Delete(mid);

      return View("_PageModules", await BuildPageEditorViewModel(viewModel.Page, null, true));
    }

    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public ActionResult DeleteModule(Guid mid)
    {
      // this action exists to return an error if a user tries to enter the DeleteModule url into their browser
      return BadRequest();
    }

    [HttpGet]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public ActionResult DeletePageModuleInline(Guid mid)
    {
      // this action exists to return an error if a user tries to enter the DeletePageModuleInline url into their browser
      return BadRequest();
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> DeletePageModuleInline(ViewModels.Admin.PageEditor viewModel, Guid mid)
    {
      await this.PageModuleManager.Delete(mid);

      return Ok();
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> MoveModuleDown(ViewModels.Admin.PageEditor viewModel, Guid mid)
    {
      await this.PageModuleManager.MoveDown(viewModel.Page, mid);

      viewModel = await BuildPageEditorViewModel(viewModel.Page, null, true);

      return View("_PageModules", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> MoveModuleUp(ViewModels.Admin.PageEditor viewModel, Guid mid)
    {
      await this.PageModuleManager.MoveUp(viewModel.Page, mid);

      viewModel = await BuildPageEditorViewModel(viewModel.Page, null, true);

      return View("_PageModules", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_EDIT_POLICY)]
    public async Task<ActionResult> AddPagePermissionRole(ViewModels.Admin.PageEditor viewModel)
    {
      if (viewModel.SelectedPageRoleId != Guid.Empty)
      {
        Role role = await this.RoleManager.Get(viewModel.SelectedPageRoleId);
        viewModel.Page.Permissions = await RebuildPermissions(viewModel.PagePermissions);

        await this.PageManager.CreatePermissions(this.Context.Site, viewModel.Page, role);
      }

      viewModel.PagePermissions = viewModel.Page.Permissions.ToPermissionsList(this.Context.Site);
      viewModel.DisableCopy = true;

      return View("Editor", await BuildPageEditorViewModel(viewModel.Page, viewModel.PagePermissions, false));
    }

    [HttpGet]
    public async Task<ActionResult> GetChildPages(Guid id)
    {
      ViewModels.Admin.PageIndexPartial viewModel = new();

      viewModel.FromPage = await this.PageManager.Get(id);

      viewModel.Pages = await this.PageManager.GetAdminMenu
        (
          this.Context.Site,
          await this.PageManager.Get(id),
          ControllerContext.HttpContext.User,
          1
        );

      return View("PageMenu", viewModel);
    }

    private async Task<ViewModels.Admin.PageIndex> BuildIndexViewModel(Guid pageId, Boolean openPage)
    {
      ViewModels.Admin.PageIndex viewModel = new();

      viewModel.Pages = await this.PageManager.GetAdminMenu
        (
          this.Context.Site,
          null,
          ControllerContext.HttpContext.User,
          pageId
        );

      viewModel.PageId = pageId;
      viewModel.OpenPage = openPage;

      viewModel.PageTemplates = CreateTemplatesSelectList(await ListPageTemplates());

      return viewModel;
    }

    private async Task<ViewModels.Admin.PageIndex> BuildIndexViewModel()
    {
      ViewModels.Admin.PageIndex viewModel = new();

      viewModel.Pages = await this.PageManager.GetAdminMenu
        (
          this.Context.Site,
          null,
          ControllerContext.HttpContext.User,
          0
        );
      viewModel.PageTemplates = CreateTemplatesSelectList(await ListPageTemplates());

      return viewModel;
    }

    private SelectList CreateTemplatesSelectList(List<Nucleus.Abstractions.Models.FileSystem.File> files)
    {
      return new SelectList
      (
        files.Select(file => new
        {
          Id = file.Id,
          Name = System.IO.Path.GetFileNameWithoutExtension(file.Name)
        }),
        "Id",
        "Name"
      );
    }

    private const string SYSTEM_FOLDER = "system-files";
    private const string PAGE_TEMPLATES_FOLDER = "page-templates";

    private async Task<List<Nucleus.Abstractions.Models.FileSystem.File>> ListPageTemplates()
    {
      Folder pageTemplatesFolder = await this.GetTemplatesFolder();

      return (await this.FileSystemManager.ListFolder(this.Context.Site, pageTemplatesFolder.Id, "(.*).xml"))
        .Files;
    }

    private async Task SaveTemplate(System.IO.Stream templateStream, string filename)
    {
      Folder pageTemplatesFolder = await this.GetTemplatesFolder();
      await this.FileSystemManager.SaveFile(this.Context.Site, pageTemplatesFolder.Provider, pageTemplatesFolder.Path, filename, templateStream, true);
    }

    private async Task<Folder> GetTemplatesFolder()
    {
      Folder systemFolder;
      Folder pageTemplatesFolder;

      string key = this.FileSystemManager.ListProviders().First().Key;

      try
      {
        systemFolder = await this.FileSystemManager.GetFolder(this.Context.Site, key, SYSTEM_FOLDER);
      }
      catch (FileNotFoundException)
      {
        _ = await this.FileSystemManager.CreateFolder(this.Context.Site, key, "/", SYSTEM_FOLDER);
      }

      try
      {
        pageTemplatesFolder = await this.FileSystemManager.GetFolder(this.Context.Site, key, SYSTEM_FOLDER + "/" + PAGE_TEMPLATES_FOLDER);
      }
      catch (FileNotFoundException)
      {
        pageTemplatesFolder = await this.FileSystemManager.CreateFolder(this.Context.Site, key, SYSTEM_FOLDER, PAGE_TEMPLATES_FOLDER);
      }

      return pageTemplatesFolder;
    }

    private async Task<ViewModels.Admin.PageModuleCommonSettingsEditor> BuildPageModuleCommonSettingsViewModel(Page page, PageModule module, PermissionsList modulePermissions, Boolean getPermissions)
    {
      ViewModels.Admin.PageModuleCommonSettingsEditor viewModel = new();// await BuildPageEditorViewModel(page, null, false);
      viewModel.Module = module;
      viewModel.ModulePermissions = modulePermissions;

      viewModel.AvailableModules = await GetAvailableModules();

      viewModel.AvailableModuleRoles = await GetAvailableRoles(viewModel.Module?.Permissions);
      viewModel.ModulePermissionTypes = await this.PageModuleManager.ListModulePermissionTypes();

      viewModel.ModuleContainers = (await this.ContainerManager.List()).InsertDefaultListItem("(page default)");

      viewModel.AvailablePanes = (await this.LayoutManager.ListLayoutPanes(page.LayoutDefinition ?? this.Context.Site.DefaultLayoutDefinition)).Append("None");

      // Set modules with an invalid pane to "Missing Pane"
      if (!viewModel.AvailablePanes.Contains(viewModel.Module.Pane))
      {
        viewModel.Module.Pane = "None";
      }

      if (getPermissions)
      {
        // read permissions from the database to initialize the viewModel
        module.Permissions = await this.PageModuleManager.ListPermissions(module);
        viewModel.ModulePermissions = module.Permissions.ToPermissionsList(this.Context.Site);
      }
      else
      {
        // re-populate missing data from existing viewModel permissions
        await RebuildPermissions(viewModel.ModulePermissions);
      }

      viewModel.ModuleContainerStyles = await this.ContainerManager.ListContainerStyles(this.Context.Site, page, module.ContainerDefinition);

      // add "custom" list item for supported syntax types
      foreach (ContainerStyle currentStyle in viewModel.ModuleContainerStyles)
      {
        if (currentStyle.Syntax == "<color>")
        {
          currentStyle.Values.Insert(0, new() { Name = CUSTOM_CONTAINER_STYLE_VALUE, CssClass = CUSTOM_CONTAINER_STYLE_VALUE, Title = "Custom" });
        }
      }

      // populate each container style's SelectedValue/CustomValue properties
      if (viewModel.ModuleContainerStyles?.Any() == true)
      {
        if (!String.IsNullOrEmpty(viewModel.Module.AutomaticClasses))
        {
          string[] cssClasses = viewModel.Module.AutomaticClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

          if (cssClasses.Any())
          {
            foreach (ContainerStyle style in viewModel.ModuleContainerStyles)
            {
              foreach (ContainerStyleValue value in style.Values)
              {
                if (cssClasses.Contains(value.CssClass))
                {
                  style.SelectedValue = value.CssClass;
                  break;
                }
              }
            }
          }
        }

        if (!String.IsNullOrEmpty(viewModel.Module.AutomaticStyles))
        {
          string[] cssStyles = viewModel.Module.AutomaticStyles.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

          if (cssStyles.Any())
          {
            foreach (ContainerStyle style in viewModel.ModuleContainerStyles)
            {
              foreach (string cssStyle in cssStyles)
              {
                foreach (ContainerStyleValue value in style.Values)
                {
                  string token = $"--{style.Name}:";
                  if (cssStyle.StartsWith(token))
                  {
                    style.CustomValue = cssStyle[token.Length..];
                    style.SelectedValue = CUSTOM_CONTAINER_STYLE_VALUE;
                    break;
                  }
                }
              }         
            }
          }
        }

        foreach (ContainerStyle style in viewModel.ModuleContainerStyles)
        { 
          // default value
          if (style.Syntax == "<color>" && String.IsNullOrEmpty(style.CustomValue))
          {
            style.CustomValue = "#000000";
          }     
        }
      }

      viewModel.ModuleContainerStyleGroups = viewModel.ModuleContainerStyles
        .Select(style => style.Group)
        .Distinct();

      return viewModel;
    }

    private async Task<ViewModels.Admin.PageModuleEditor> BuildPageModuleViewModel(Guid mid)
    {
      ViewModels.Admin.PageModuleEditor viewModel = new()
      {
        Module = await this.PageModuleManager.Get(mid)
      };

      return viewModel;
    }

    private async Task<ViewModels.Admin.PageEditor> BuildPageEditorViewModel(Page page, PermissionsList pagePermissions, Boolean getPermissions)
    {
      ViewModels.Admin.PageEditor viewModel = new();

      viewModel.Page = page;
      viewModel.PagePermissions = pagePermissions;

      // if the page was populated by MVC, it won't have any modules.  Checking Modules.Any() before calling .ListModules is a small improvement in performance
      if (!viewModel.Page.Modules.Any())
      {
        viewModel.Page.Modules = await this.PageManager.ListModules(viewModel.Page);
      }

      viewModel.ParentPageMenu = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, page.ParentId);
      viewModel.LinkPageMenu = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, viewModel.Page.LinkPageId);

      //viewModel.AvailableModules = await GetAvailableModules();
      viewModel.AvailablePageRoles = await GetAvailableRoles(viewModel.Page?.Permissions);
      viewModel.PagePermissionTypes = await this.PageManager.ListPagePermissionTypes();

      if (viewModel.Page.LinkFileId.HasValue)
      {
        try
        {
          viewModel.SelectedLinkFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.Page.LinkFileId.Value);
        }
        catch (FileNotFoundException)
        {
          viewModel.SelectedLinkFile = null;
        }
      }

      if (getPermissions)
      {
        // read permissions from the database to initialize the viewModel
        page.Permissions = await this.PageManager.ListPermissions(page);
        viewModel.PagePermissions = page.Permissions.ToPermissionsList(this.Context.Site);
      }
      else
      {
        // re-populate missing data from existing viewModel permissions
        await RebuildPermissions(viewModel.PagePermissions);
      }

      viewModel.CanDeletePage = User.HasEditPermission(this.Context.Site, viewModel.Page);

      // re-read the selected layout (viewmodel will only contain ID, and we need RelativePath for the call to ListLayoutPanes)
      if (viewModel.Page.LayoutDefinition != null)
      {
        viewModel.Page.LayoutDefinition = await this.LayoutManager.Get(viewModel.Page.LayoutDefinition.Id);
      }

      viewModel.Layouts = (await this.LayoutManager.List()).InsertDefaultListItem("(site default)");
      viewModel.PageContainers = (await this.ContainerManager.List()).InsertDefaultListItem("(site default)");

      viewModel.PagePanes = (await this.LayoutManager.ListLayoutPanes(page.LayoutDefinition ?? this.Context.Site.DefaultLayoutDefinition)).Append("None");

      // Set modules with an invalid pane to "Missing Pane"
      foreach (PageModule moduleMissingPane in viewModel.Page.Modules.Where(module => !viewModel.PagePanes.Contains(module.Pane)))
      {
        moduleMissingPane.Pane = "None";
      }

      if (!page.Routes.Any())
      {
        page.Routes.Add(new PageRoute());
      }

      return viewModel;
    }

    private async Task<IEnumerable<SelectListItem>> GetAvailableModules()
    {
      List<SelectListItem> results = new();

      IEnumerable<ModuleDefinition> availableModules = await this.PageModuleManager.ListModuleDefinitions();

      // get a list of distinct category names
      IEnumerable<string> moduleCategories = availableModules
        .SelectMany(module => String.IsNullOrEmpty(module.Categories) ? new string[] { "" } : module.Categories.Split(','))        
        .Distinct()
        .OrderBy(category => category);

      // create a keyed list of SelectListGroup items for each category.  This is so that we can assign each module in the same category to the same 
      // SelectListGroup object.  Each SelectListGroup is an <optgroup> in html.
      Dictionary<string, SelectListGroup> selectListGroups = 
        moduleCategories.ToDictionary(category => category, category => new SelectListGroup() { Name = category });

      // get a list of modules for each category.  Modules can appear in more than one category.
      Dictionary<string, IOrderedEnumerable<ModuleDefinition>> categories = moduleCategories
        .ToDictionary
        (
          category => category, 
          category => 
            availableModules
              .Where(module => (module.Categories ?? "").Split(',').Contains(category))
              .OrderBy(module => module.FriendlyName ?? "")
        );

      // Create SelectListItem objects for each category/module combination.  SelectListItem objects are rendered as <option> elements in Html.
      foreach (KeyValuePair<string, IOrderedEnumerable<ModuleDefinition>> item in categories)
      {
        results.AddRange(item.Value.Select(module => new SelectListItem(module.FriendlyName, module.Id.ToString())
        {
          Selected = module.Id == Guid.Parse("b516d8dd-c793-4776-be33-902eb704bef6"), // this makes the html module the default 
          Group = String.IsNullOrEmpty(item.Key) ? null : selectListGroups[item.Key]
        }));
      }

      return results;
    }

    private async Task<IEnumerable<SelectListItem>> GetAvailableRoles(List<Permission> existingPermissions)
    {
      IEnumerable<Role> availableRoles = (await this.RoleManager.List(this.Context.Site))
        .Where
        (
          role => role.Id != this.Context.Site.AdministratorsRole?.Id && (existingPermissions == null || !existingPermissions.Where(item => item.Role.Id == role.Id).Any())
        )
        .OrderBy(role => role.Name);

      IEnumerable<string> roleGroups = availableRoles
        .Where(role => role.RoleGroup != null)
        .Select(role => role.RoleGroup.Name)
        .Distinct()
        .OrderBy(name => name);

      Dictionary<string, SelectListGroup> groups = roleGroups.ToDictionary(name => name, name => new SelectListGroup() { Name = name });

      return availableRoles.Select(role => new SelectListItem(role.Name, role.Id.ToString())
      {
        Group = groups.Where(group => role.RoleGroup != null && role.RoleGroup.Name == group.Key).FirstOrDefault().Value
      })
      .OrderBy(selectListItem => selectListItem.Group?.Name);
    }


    private async Task<List<Permission>> RebuildPermissions(PermissionsList permissions)
    {
      if (permissions == null) return null;

      foreach (KeyValuePair<Guid, PermissionsListItem> rolePermissions in permissions)
      {
        foreach (Permission permission in rolePermissions.Value.Permissions)
        {
          permission.Role = await this.RoleManager.Get(rolePermissions.Key);

          rolePermissions.Value.Role = permission.Role;
        }
      }

      return permissions.ToList();
    }
  }
}
