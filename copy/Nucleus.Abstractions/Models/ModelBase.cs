using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	public class ModelBase
	{
		public Guid AddedBy { get; set; }
		public DateTime DateAdded { get; set; }
		public Guid ChangedBy { get; set; }
		public DateTime DateChanged { get; set; }
	}
}
