using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Publish.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.Modules.Publish.ViewModels
{
	public class Viewer
	{
		public Context Context { get; set; }
		public Guid ModuleId { get; set; }

    public Settings Settings { get; set; } = new();

    public PagedResult<Models.Article> Articles { get; set; }

    public IEnumerable<Models.Article> PrimaryArticles { get; set; }
    public IEnumerable<Models.Article> SecondaryArticles { get; set; }

    public string MasterLayoutPath { get; set; }
    public string PrimaryArticleLayoutPath { get; set; }
    public string SecondaryArticleLayoutPath { get; set; }
  }
}
