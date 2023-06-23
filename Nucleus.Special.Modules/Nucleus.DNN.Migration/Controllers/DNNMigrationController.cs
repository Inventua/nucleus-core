using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.DNN.Migration.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.storage.irelationalcommand.executereaderasync?view=efcore-7.0

namespace Nucleus.DNN.Migration.Controllers;

[Extension("DNNMigration")]
public class DNNMigrationController : Controller
{
  private Context Context { get; }
  private DNNMigrationManager DNNMigrationManager { get; }

  public DNNMigrationController(Context Context, DNNMigrationManager dnnMigrationManager)
  {
    this.Context = Context;
    this.DNNMigrationManager = dnnMigrationManager;
  }

  [HttpGet]
  public async Task <ActionResult> Index()
  {
    return View("Index", await BuildViewModel());
  }

  private async Task <ViewModels.Index> BuildViewModel()
  {
    ViewModels.Index viewModel = new();

    viewModel.Version = await this.DNNMigrationManager.GetDNNVersion();
     
    return viewModel;
  }
}