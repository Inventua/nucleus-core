using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Client
{
	internal class Routes
	{
		private const string BASE = "/saml2/sp/";

		public const string AUTHENTICATE = BASE + "authenticate";
		public const string ASSERTION_CONSUMER = BASE + "callback";
		public const string METADATA = BASE + "metadata";

		public const string SINGLE_LOGOUT = BASE + "singlelogout";
		public const string LOGOUT = BASE + "logout";
		public const string LOGGED_OUT = BASE + "loggedout";

	}	
}
