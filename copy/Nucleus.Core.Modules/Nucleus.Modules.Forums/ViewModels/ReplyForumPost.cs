using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.ViewModels
{
	public class ReplyForumPost
	{
		public Forum Forum { get; set; }
		public Post Post { get; set; }
		public Reply Reply { get; set; }
		
		public Boolean CanAttach { get; set; }		
		public Boolean CanSubscribe { get; set; }
		
	}
}
