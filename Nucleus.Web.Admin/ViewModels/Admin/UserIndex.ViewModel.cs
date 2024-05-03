using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;

namespace Nucleus.Web.ViewModels.Admin
{
	public class UserIndex
	{
		public Nucleus.Abstractions.Models.Paging.PagedResult<Nucleus.Abstractions.Models.User> Users { get; set; } = new() { PageSize = 20 };
		public string SearchTerm { get; set; }

		//public Nucleus.Abstractions.Models.Paging.PagedResult<Nucleus.Abstractions.Models.User> SearchResults { get; set; } = new();
		public Site Site { get; set; }

    public IEnumerable<SelectListItem> FilterRoles { get; set; }

    public UserFilterOptions FilterSelections { get; set; } = new();
	}

  public class UserFilterOptions
  {
    public enum ApprovedFilter
    {
      All = ApprovedOnly | NotApprovedOnly,
      [Display(Name = "Approved")]
      ApprovedOnly = 1,
      [Display(Name = "Not Approved")]
      NotApprovedOnly = 2
    }

    public enum VerifiedFilter
    {
      All = VerifiedOnly | NotVerifiedOnly,
      [Display(Name = "Verified")]
      VerifiedOnly = 1,
      [Display(Name = "Not Verified")]
      NotVerifiedOnly = 2
    }

    public ApprovedFilter Approved { get; set; } = ApprovedFilter.All;
    public VerifiedFilter Verified { get; set; } = VerifiedFilter.All;

    public Guid? RoleId { get; set; }

  }
}
