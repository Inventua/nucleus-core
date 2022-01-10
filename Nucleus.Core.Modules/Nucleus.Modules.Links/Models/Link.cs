using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Links.Models
{
	public static class LinkTypes
	{
		public const string Url = "urn:url";
		public const string File = Nucleus.Abstractions.Models.FileSystem.File.URN;
		public const string Page = Nucleus.Abstractions.Models.Page.URN;
	}

	public class Link
	{
		public Guid Id { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public string LinkType { get; set; }
		public int SortOrder { get; set; }
		public LinkUrl LinkUrl { get; set; }
		public LinkPage LinkPage { get; set; }
		public LinkFile LinkFile { get; set; }
		public ListItem Category { get; set; }
	}
}
