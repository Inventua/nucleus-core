using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	public class HtmlEditorScript
	{
		public enum Types
		{
			stylesheet,
			javascript
		}

		public Types Type { get; private set; }
		public string Path { get; private set; }
	}
}
