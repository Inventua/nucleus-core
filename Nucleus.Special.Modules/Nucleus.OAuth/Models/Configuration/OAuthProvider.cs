using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Models.Configuration
{
	public class OAuthProvider
	{
		public string Name { get; set; }
		public string Type { get; set; }

		public List<MapJsonKey> MapJsonKeys { get; set; } = new();
		public List<string> Scope { get; set; } = new();	

	}
}
