using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Modules.PageLinks.Controllers;

[Extension("PageLinks")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class PageLinksSettingsController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }
  private PageLinksManager PageLinksManager { get; }

  public PageLinksSettingsController(Context Context, IPageModuleManager pageModuleManager, PageLinksManager pageLinksManager)
  {
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
    this.PageLinksManager = pageLinksManager;
  }

  [HttpGet]
  public async Task<ActionResult> Settings()
  {
    return View("Settings", await BuildSettingsViewModel(null));
  }

  [HttpPost]
  public async Task<ActionResult> AddPageLink(ViewModels.Settings viewModel)
  {
    if (viewModel.PageLinks == null)
    {
      viewModel.PageLinks = [];
    }

    viewModel.PageLinks.Add(new Models.PageLink());

    return View("_PageLinksList", await BuildSettingsViewModel(viewModel));
  }

  /// <summary>
  /// Remove the page link specified by id from the currently selected page's links.  
  /// </summary>
  /// <param name="viewModel"></param>
  /// <param name="id"></param>
  /// <returns></returns>
  /// <remarks>
  /// If a link with the specified id is not present in the currently selected page's links, no action is taken and no exception is 
  /// generated.  The "delete" occurs within the viewModel only - the Save action must be called in order to commit the delete operation
  /// to the database.
  /// </remarks>
  [HttpPost]
  public async Task<ActionResult> RemovePageLink(ViewModels.Settings viewModel, Guid id)
  {
    foreach (Models.PageLink link in viewModel.PageLinks)
    {
      if (link.Id == id)
      {
        viewModel.PageLinks.Remove(link);
        break;
      }
    }

    ModelState.Clear();
    return View("_PageLinksList", await BuildSettingsViewModel(viewModel));
  }

  /// <summary>
  /// Moves the page link <see cref="Models.PageLink.SortOrder"/> by swapping with the immediate previous page link.
  /// </summary>
  /// <param name="viewModel"></param>
  /// <param name="id"></param>
  /// <returns></returns>
  [HttpPost]
  public async Task<ActionResult> MoveUp(ViewModels.Settings viewModel, Guid id)
  {
    await this.PageLinksManager.MoveUp(this.Context.Module, id);

    ModelState.Clear();
    return View("_PageLinksList", await BuildSettingsViewModel(viewModel));
  }

  /// <summary>
  /// Moves the page link <see cref="Models.PageLink.SortOrder"/> by swapping with the immediate page link after it.
  /// </summary>
  /// <param name="viewModel"></param>
  /// <param name="id"></param>
  /// <returns></returns>
  [HttpPost]
  public async Task<ActionResult> MoveDown(ViewModels.Settings input, Guid id)
  {
    await this.PageLinksManager.MoveDown(this.Context.Module, id);

    ModelState.Clear();
    return View("_PageLinksList", await BuildSettingsViewModel(input));
  }

  /// <summary>
  /// Save the settings and manual page links.
  /// </summary>
  /// <param name="viewModel"></param>
  /// <returns></returns>
  /// <remarks>Manual page links are saved regardless of mode of operation.</remarks>
  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
  {
    // Page links tab
    await this.PageLinksManager.Save(this.Context.Module, viewModel.PageLinks);

    // Settings tab
    viewModel.SetSettings(this.Context.Module);

    await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

    return Ok();
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new()
      {
        PageLinks = await this.PageLinksManager.List(this.Context.Module)
      };
    }
    else
    {
      // copy new sort orders to existing viewModel
      List<Models.PageLink> sortedLinks = await this.PageLinksManager.List(this.Context.Module);
      foreach (Models.PageLink sortedPageLink in sortedLinks)
      {
        Models.PageLink pageLink = viewModel.PageLinks.Where(link => link.Id == sortedPageLink.Id).FirstOrDefault();
        if (pageLink != null)
        {
          pageLink.SortOrder = sortedPageLink.SortOrder;
        }
      }
      viewModel.PageLinks = viewModel.PageLinks.OrderBy(pageLink => pageLink.SortOrder).ToList();
    }

    viewModel.GetSettings(this.Context.Module);

    return viewModel;
  }

}