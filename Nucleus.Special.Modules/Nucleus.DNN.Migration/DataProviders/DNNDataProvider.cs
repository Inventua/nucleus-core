using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using DocumentFormat.OpenXml.Spreadsheet;
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
    return await this.Context.RoleGroups
      .Where(group => group.PortalId == portalId && group.Roles.Any())
      .OrderBy(group => group.RoleGroupName)
      .ToListAsync();
  }

  public async Task<List<Models.DNN.Role>> ListRoles(int portalId)
  {
    string[] RESERVED_ROLES = { "Administrators", "Registered Users" };

    return await this.Context.Roles
      .Where(role => role.PortalId == portalId && role.Users.Any() && !RESERVED_ROLES.Contains(role.RoleName))
      .OrderBy(role => role.RoleName)
      .Include(role => role.RoleGroup)
      .ToListAsync();
  }

  public async Task<List<Models.DNN.User>> ListUsers(int portalId)
  {   
    //var test= this.Context.Users
    //  .Where(user => user.PortalId == portalId && !user.IsSuperUser && user.UserPortal.Authorised)
    //  .Include(user => user.Roles)
    //  .Include(user => user.ProfileProperties)
    //  .Include(user => user.UserPortal)
    //  .AsSplitQuery();

    return await this.Context.Users
      .Where(user => user.UserPortal.PortalId == portalId && !user.IsSuperUser && user.UserPortal.Authorised)
      .Include(user => user.Roles)
      .Include(user => user.ProfileProperties)
      .Include(user => user.UserPortal)
      .OrderBy(user => user.UserName)
      .AsSplitQuery()
      .ToListAsync();
  }
}
