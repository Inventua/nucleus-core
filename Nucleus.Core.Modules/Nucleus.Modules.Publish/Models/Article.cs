using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish.Models
{
	public class Article : ModelBase
	{
		public const string URN = "urn:nucleus:entities:publish-article";
		public Guid Id { get; set; }
		public string Title { get; set; }
		public string SubTitle { get; set; }
		public string Description { get; set; }
		public string Summary { get; set; }
		public string Body { get; set; }
		public File ImageFile { get; set; }
		public DateTime? PublishDate { get; set; }
		public DateTime? ExpireDate { get; set; }
		public Boolean Enabled { get; set; } = true;
		public Boolean Featured  { get; set; }
		public List<Attachment> Attachments { get; set; } = new();
		public List<Category> Categories { get; set; } = new();

		//public string EncodedTitle()
		//{
		//	return this.Title.Replace(' ', '-');
		//}
	}
}
