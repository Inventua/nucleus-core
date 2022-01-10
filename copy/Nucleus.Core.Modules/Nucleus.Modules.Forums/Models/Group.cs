using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
	public class Group
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public long SortOrder { get; set; }
		public Settings Settings { get; set; }
		public IList<Forum> Forums { get; set; }
		public List<Permission> Permissions { get; set; } = new();
		public Folder AttachmentsFolder { get; set; }
	}
}
