using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
          
namespace Nucleus.DNN.Migration.Models.Modules
{
	public class Document
	{
		public const string URN = "urn:nucleus:entities:document";

		public Guid Id { get; set; }
		public string Title { get; set; }
		public string Description{ get; set; }
		public ListItem Category { get; set; }
		public int SortOrder { get; set; }
		public File File { get; set; }
				
	}
}
