using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
	public class Forum
	{
		public const string URN = "urn:nucleus:entities:forum";

		public Guid Id { get; set; }		
		public string Name { get; set; }
		public string Description { get; set; }
		public long SortOrder { get; set; }
		public Boolean UseGroupSettings { get; set; }
		public Settings Settings { get; set; }
		public List<Permission> Permissions { get; set; } = new();
		public ForumStatistics Statistics { get; set; }
		public Folder AttachmentsFolder { get; set; }

		public string EncodedName ()
		{
			return Name.Replace(' ', '-');
		}

	}
}
