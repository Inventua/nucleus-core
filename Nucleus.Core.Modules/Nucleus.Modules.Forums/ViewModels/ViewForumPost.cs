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
	public class ViewForumPost
	{    
    public Page Page { get; set; }
		public Forum Forum { get; set; }
		public Post Post { get; set; }

		public IList<Reply> Replies { get; set; }

		public PostSubscription Subscription { get; set; }

    public Boolean IsSubscribedToForum { get; set; }

    public Boolean CanEditPost { get; set; }
		public Boolean CanReply { get; set; }
		public Boolean CanAttach { get; set; }
		public Boolean CanPinPost { get; set; }
		public Boolean CanLockPost { get; set; }

		public Boolean CanApprovePost { get; set; }
		public Boolean CanSubscribe { get; set; }
		public Boolean CanDeletePost { get; set; }

		

	}
}
