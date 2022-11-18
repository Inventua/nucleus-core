using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Server
{
	internal class Routes
	{
		private const string BASE = "/saml2/idp/";

		public const string LOGIN = BASE + "login";
		public const string RESPOND = BASE + "respond";
		public const string METADATA = BASE + "metadata";
		public const string ARTIFACT = BASE + "artifact";
		public const string LOGOUT = BASE + "logout";
	}	
}
