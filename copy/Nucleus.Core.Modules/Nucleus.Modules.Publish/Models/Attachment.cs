using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish.Models
{
	public class Attachment
	{
		public Guid Id { get; set; }
		public File File { get; set; }
	}
}
