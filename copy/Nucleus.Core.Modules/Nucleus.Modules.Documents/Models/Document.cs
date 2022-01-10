using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Modules.Documents.Models
{
	public class Document
	{
		public Guid Id { get; set; }
		public string Title { get; set; }
		public string Description{ get; set; }
		public ListItem Category { get; set; }
		public long SortOrder { get; set; }
		public File File { get; set; }
				
	}
}
