using Nucleus.Abstractions.Models;
using System;

namespace Nucleus.Modules.Publish.Models
{
	public class Category
	{
		public Guid Id { get; set; }
		public Guid ArticleId { get; set; }
		public ListItem CategoryListItem { get; set; }
	}
}
