using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.DataProviders;
/// <summary>
/// Module data provider.
/// </summary>
/// <remarks>
/// This class implements the IDNNMigrationDataProvider interface, and inherits the base Nucleus entity framework data provider class.
/// </remarks>
public class DNNMigrationDataProvider : Nucleus.Data.EntityFramework.DataProvider
{
  protected IEventDispatcher EventManager { get; }
  protected new DNNMigrationDbContext Context { get; }

  public DNNMigrationDataProvider(DNNMigrationDbContext context, IEventDispatcher eventManager, ILogger<DNNMigrationDataProvider> logger) : base(context, logger)
  {
    this.EventManager = eventManager;
    this.Context = context;
  }

  public async Task<Boolean> ForumExists(string groupName, string forumName)
  {
    Models.RecordCount recordCount = await this.Context.Set<RecordCount>().FromSqlInterpolated($"SELECT COUNT(*) AS Count FROM ForumGroups, Forums WHERE Forums.ForumGroupId = ForumGroups.Id AND ForumGroups.Name={groupName} AND Forums.Name={forumName}")
      .FirstOrDefaultAsync();

    return recordCount.Count > 0;
  }

  public async Task<ForumInfo> GetNucleusForumInfo(string groupName, string forumName)
  {
    Models.ForumInfo value = await this.Context.ForumInfo
      .Where(forum => forum.Name == forumName && forum.ForumGroup.Name == groupName)
      .Include(forum => forum.ForumGroup)
      .FirstOrDefaultAsync();

    //Models.ForumInfo value = await this.Context.Set<ForumInfo>().FromSqlInterpolated($"SELECT Id AS ForumId, ModuleId FROM Forums LEFT INNER JOIN ForumGroups ON Forums.ForumGroupId = ForumGroups.Id AND ForumGroups.Name={groupName} WHERE Forums.Name={forumName}")
    //  .FirstOrDefaultAsync();

    return value;
  }
}