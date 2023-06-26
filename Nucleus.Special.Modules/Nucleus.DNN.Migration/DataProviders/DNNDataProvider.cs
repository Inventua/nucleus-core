using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.DataProviders;

public class DNNDataProvider : Nucleus.Data.EntityFramework.DataProvider//, IDNNDataProvider
{
  protected new DNNDbContext Context { get; }

  public DNNDataProvider(DNNDbContext context, ILogger<DNNMigrationDataProvider> logger) : base(context, logger)
  {
    this.Context = context;
  }

  public async Task<Models.DNN.Version> GetVersion()
  {
    return await this.Context.Version
      .OrderByDescending(version => version.Major)
        .ThenByDescending(version => version.Minor)
        .ThenByDescending(version => version.Build)
      .FirstOrDefaultAsync();
  }

  public async Task<List<Models.DNN.Portal>> ListPortals()
  {
    return await this.Context.Portals
      .ToListAsync();
  }

  public async Task<List<Models.DNN.RoleGroup>> ListRoleGroups(int portalId)
  {
    List<Models.DNN.RoleGroup> results = await this.Context.RoleGroups
      .Where(group => group.PortalId == portalId && group.Roles.Any())
      .OrderBy(group => group.RoleGroupName)
      .ToListAsync();

    foreach (var roleGroup in results)
    {
      roleGroup.RoleCount = await this.Context.Roles
        .Where(role => role.RoleGroup.RoleGroupId == roleGroup.RoleGroupId)
        .CountAsync();
    }

    return results;
  }

  public async Task<List<Models.DNN.Role>> ListRoles(int portalId)
  {
    List<Models.DNN.Role> results = await this.Context.Roles
      .Where(role => role.PortalId == portalId)
      .OrderBy(role => role.RoleName)
      .Include(role => role.RoleGroup)
      .ToListAsync();

    foreach (var role in results) 
    {
      role.UserCount = await this.Context.UserRoles
        .Where(userRole => userRole.RoleId == role.RoleId)
        .CountAsync();
    }

    return results;
  }

  public async Task<List<Models.DNN.User>> ListUsers(int portalId)
  {   
    return await this.Context.Users
      .Where(user => user.UserPortal.PortalId == portalId)
      .Include(user => user.Roles)
      .Include(user => user.ProfileProperties)
      .Include(user => user.UserPortal)
      .OrderBy(user => user.UserName)
      .AsSplitQuery()
      .ToListAsync();
  }

  public async Task<List<Models.DNN.Page>> ListPages(int portalId)
  { 
    return await this.Context.Pages
      .Where(page => 
        page.PortalId == portalId && 
        page.PageName != "Admin" && !page.TabPath.StartsWith("//Admin") && // exclude "Admin" page, and descendants
        !page.IsDeleted 
      )
      .OrderBy(page => page.Level)
        .ThenBy(page => page.ParentId)
        .ThenBy(page => page.PageName)
      .AsSplitQuery()
      .ToListAsync();
  }
}
