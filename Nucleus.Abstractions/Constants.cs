using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	/// <summary>
	/// General constant values
	/// </summary>
	public static class RoutingConstants
	{
		/// <summary>
		/// Key for the area route.
		/// </summary>
		public const string AREA_ROUTE_NAME = "route:area";
		/// <summary>
		/// Key for the API route.
		/// </summary>
		public const string API_ROUTE_NAME = "route:module-api";
		/// <summary>
		/// Key for the extensions route.
		/// </summary>
		public const string EXTENSIONS_ROUTE_NAME = "route:extensions";
		
		/// <summary>
		/// Key for the sitemap route.
		/// </summary>
		public const string SITEMAP_ROUTE_NAME = "route:sitemap";

		/// <summary>
		/// Key for the error route.
		/// </summary>
		public const string ERROR_ROUTE_NAME = "route:error";
	
		/// <summary>
		/// Root path for the admin route.
		/// </summary>
		public const string ADMIN_ROUTE_PATH = "admin";
		/// <summary>
		/// Root path for the Api route.
		/// </summary>
		public const string API_ROUTE_PATH = "api";
		/// <summary>
		/// Root path for the extensions route.
		/// </summary>
		public const string EXTENSIONS_ROUTE_PATH = "extensions";
		
		/// <summary>
		/// Path for the site map route.
		/// </summary>
		public const string SITEMAP_ROUTE_PATH = "sitemap.xml";

		/// <summary>
		/// Path for the error route.
		/// </summary>
		public const string ERROR_ROUTE_PATH = "error-handler";
				
		/// <summary>
		/// Reserved route names.
		/// </summary>
		// the "pages" route isn't currently used for anything, but is reserved in case we want to use it later
		public static readonly string[] RESERVED_ROUTES = { ADMIN_ROUTE_PATH, EXTENSIONS_ROUTE_PATH, "pages", SITEMAP_ROUTE_PATH, ERROR_ROUTE_PATH };
	}

	/// <summary>
	/// Constants for text file logging components.
	/// </summary>
	public class LogFileConstants
	{

		/// <summary>
		/// Date Format used for log file names
		/// </summary>
		public const string DATE_FILENAME_FORMAT = "dd-MMM-yyyy UTC";
		
		/// <summary>
		/// Date/Time Format used for log file names
		/// </summary>
		public const string DATETIME_FILENAME_FORMAT = "dd-MMM-yyyy HH-mm UTC";

		/// <summary>
		/// Regex used to parse log file into date and computer name
		/// </summary>
		public const string LOGFILE_REGEX = "(?<logdate>[0-9]{2}-[A-Za-z]{3}-[0-9]{4}[ 0-9-]{0,6} UTC)_(?<computername>.*).log";

	}
}
