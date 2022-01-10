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
	public class Settings
	{

		public IList<Document> Documents { get; set; }

		public List CategoryList { get; set; }
		public IEnumerable<List> Lists { get; set; }
		public Boolean AllowSorting { get; set; }
		public Folder SelectedFolder { get; set; }
		public string Layout { get; set; }
		public List<string> Layouts { get; set; }

		public Boolean ShowCategory { get; set; }
		public Boolean ShowSize { get; set; }
		public Boolean ShowDescription { get; set; }
		public Boolean ShowModifiedDate { get; set; }


	}
}
