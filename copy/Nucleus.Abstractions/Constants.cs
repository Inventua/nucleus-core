using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	public static class Constants
	{
		public static string AREA_ROUTE_NAME = "route:area";
		public static string API_ROUTE_NAME = "route:module-api";
		public static string EXTENSIONS_ROUTE_NAME = "route:extensions";
		public static string SITEMAP_ROUTE_NAME = "route:sitemap";

		public static string ADMIN_ROUTE_PATH = "admin";
		public static string API_ROUTE_PATH = "api";
		public static string EXTENSIONS_ROUTE_PATH = "extensions";
		public static string SITEMAP_ROUTE_PATH = "sitemap.xml";

		public static string EXTENSIONPATH_TOKEN = "~#";
		public static string VIEWPATH_TOKEN = "~!";

		public static string EDIT_COOKIE_NAME = "nucleus_editmode";
		public static string SCHEDULED_TASKS_LOG_SUBPATH = "Scheduled Tasks";

		public static string DATE_FILENAME_FORMAT = "dd-MMM-yyyy UTC";
		public static string DATETIME_FILENAME_FORMAT = "dd-MMM-yyyy HH-mm UTC";

		// the "pages" route isn't currently used for anything, but is reserved in case we want to use it later
		public static readonly string[] RESERVED_ROUTES = { ADMIN_ROUTE_PATH, EXTENSIONS_ROUTE_PATH, "pages", SITEMAP_ROUTE_PATH };
	}
}
