using Nucleus.Abstractions.Models;
using System;

namespace Nucleus.Modules.Publish.Models
{
	public class Category : ModelBase
	{
		public Guid Id { get; set; }
		public Guid ArticleId { get; set; }

		//		public Article Article { get; set; }
		public ListItem CategoryListItem { get; set; }
	}
}
