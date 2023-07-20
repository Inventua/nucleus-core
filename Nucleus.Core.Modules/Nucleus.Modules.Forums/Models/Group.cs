using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Forums.Models
{
	public class Group : ModelBase
	{
    // Groups and forums have the same permissions namespace, so use Forum.URN rather than Group.URN for permissions
    public const string URN = "urn:nucleus:entities:forum-group";

    public Guid Id { get; set; }
		public Guid ModuleId { get; set; }


		[Required]
		public string Name { get; set; }

		public int SortOrder { get; set; }

		public Settings Settings { get; set; }

		public IList<Forum> Forums { get; set; }

		public List<Permission> Permissions { get; set; } = new();
	}
}
