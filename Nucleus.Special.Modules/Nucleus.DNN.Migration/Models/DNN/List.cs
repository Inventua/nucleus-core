﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class List : DNNEntity
{
  public override int Id()
  {
    return -1;
  }

  public override string DisplayName()
  {
    return this.ListName;
  }

  [Column("PortalID")]
  public int? PortalId { get; set; }

  public string ListName { get; set; }

  public Boolean SystemList { get; set; }

  public List<ListItem> ListItems { get; set; }


}
