using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Forums.ViewModels
{
	public class ViewForum
	{
		public Page Page { get; set; }
		public Forum Forum { get; set; }
		public IList<Post> Posts { get; set; }
		public ForumSubscription Subscription { get; set; }
		public Boolean CanPost { get; set; }
		public Boolean CanSubscribe { get; set; }

	}
}
