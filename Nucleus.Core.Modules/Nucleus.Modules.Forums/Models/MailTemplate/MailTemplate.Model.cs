using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Forums.Models.MailTemplate
{
	public class Model
	{
		public List<Forum> Forums { get; } = new();
		public Site Site { get; set; }
		public Page Page { get; set; }
		public User User { get; set; }		
	}

}
