﻿using System;
using Nucleus.Abstractions.Models;
using System.Collections.Generic;

namespace Nucleus.Web.ViewModels.Admin
{
  public class SearchSettings
  {
    public ApiKey ApiKey { get; set; }
    public IEnumerable<ApiKey> ApiKeys { get; set; }

		public Boolean ClearIndex { get; set; }
		public Boolean IndexPublicPagesOnly { get; set; }
    public Boolean IndexPublicFilesOnly { get; set; }

    public Boolean IndexPagesUseSsl { get; set; } = true;
  }
}
