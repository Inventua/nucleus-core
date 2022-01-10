using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	public class HtmlEditorOptions
	{
		public const string Section = "Nucleus:HtmlEditor";

		public List<HtmlEditorScript> Scripts { get; private set; } = new();
	}
}
