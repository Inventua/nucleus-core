using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Modules.Documents.Models;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Documents.ViewModels
{
	public class Viewer
	{
		public Page Page { get; set; }

		public Boolean AllowSorting { get; set; }
		public string SortKey { get; set; }
		public Boolean SortDescending { get; set; }
		public string Layout { get; set; }
		public Boolean ShowCategory { get; set; }
		public Boolean ShowSize { get; set; }
		public Boolean ShowDescription { get; set; }
		public Boolean ShowModifiedDate { get; set; }

		public List<DocumentInfo> Documents { get; set; } = new();

		public class DocumentInfo : Document
		{
			public DateTime ModifiedDate { get; set; }
			public long Size { get; set; }
		}
	}
}
