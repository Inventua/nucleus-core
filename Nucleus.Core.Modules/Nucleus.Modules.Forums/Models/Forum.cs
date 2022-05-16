using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
	public class Forum : ModelBase 
	{
		public const string URN = "urn:nucleus:entities:forum";

		public Guid Id { get; set; }

		public Group Group {get; set; }

		public string Name { get; set; }
		public string Description { get; set; }
		public int SortOrder { get; set; }
		public Boolean UseGroupSettings { get; set; } = true;
		public Settings Settings { get; set; }
		public List<Permission> Permissions { get; set; } = new();
		public ForumStatistics Statistics { get; set; }
				
		public Settings EffectiveSettings()
		{
			if (this.UseGroupSettings)
			{
				return this.Group.Settings;
			}
			else
			{
				return this.Settings;
			}			
		}

	}
}
