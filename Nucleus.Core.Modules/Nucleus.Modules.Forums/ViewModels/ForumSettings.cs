﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Permissions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nucleus.Modules.Forums.ViewModels;

public class ForumSettings
{
  public string Message { get; set; }
  public Forum Forum { get; set; }
  public Guid GroupId { get; set; }
  public IEnumerable<MailTemplate> ImmediateMailTemplates { get; set; }
  public IEnumerable<MailTemplate> SummaryMailTemplates { get; set; }

  public Guid SelectedRoleId { get; set; }
  public IEnumerable<SelectListItem> AvailableRoles { get; set; }
  public IEnumerable<List> Lists { get; set; }
  public IEnumerable<PermissionType> ForumPermissionTypes { get; set; }
  public PermissionsList ForumPermissions { get; set; } = new();
}
