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
		/// Key for the robots.txt route.
		/// </summary>
		public const string ROBOTS_ROUTE_NAME = "route:robots.txt";

		/// <summary>
		/// Key for the merges.css route.
		/// </summary>
		public const string MERGED_CSS_ROUTE_NAME = "route:merged.css";

		/// <summary>
		/// Key for the merges.js route.
		/// </summary>
		public const string MERGED_JS_ROUTE_NAME = "route:merged.js";

		/// <summary>
		/// Key for the error route.
		/// </summary>
		public const string ERROR_ROUTE_NAME = "route:error";

    /// <summary>
    /// Route pattern used by MapControllerRoute so that we can "catch" requests for CMS pages.
    /// </summary>
    public const string DEFAULT_PAGE_PATTERN = "{*path}";

    /// <summary>
    /// Root path for the admin route.
    /// </summary>
    public const string ADMIN_ROUTE_PATH = "admin";

		/// <summary>
		/// Root path for the Api route.
		/// </summary>
		public const string API_ROUTE_PATH = "api";

		/// <summary>
		/// Root path for file requests.
		/// </summary>
		public const string FILES_ROUTE_PATH = "files";

		/// <summary>
		/// Root path for the extensions route.
		/// </summary>
		public const string EXTENSIONS_ROUTE_PATH = "extensions";
		
		/// <summary>
		/// Path for the site map route.
		/// </summary>
		public const string SITEMAP_ROUTE_PATH = "sitemap.xml";

		/// <summary>
		/// Path for the robots.txt route.
		/// </summary>
		public const string ROBOTS_ROUTE_PATH = "robots.txt";

		/// <summary>
		/// Path for the error route.
		/// </summary>
		public const string ERROR_ROUTE_PATH = "error-handler";
				
		/// <summary>
		/// Reserved routes.
		/// </summary>
		/// <remarks>
		/// Routes in the reserved routes list can't be used as page routes.
		/// </remarks>
		// the "pages" route isn't currently used for anything, but is reserved in case we want to use it later
		public static readonly string[] RESERVED_ROUTES = { ADMIN_ROUTE_PATH, EXTENSIONS_ROUTE_PATH, "pages", "oauth2", SITEMAP_ROUTE_PATH, ERROR_ROUTE_PATH, API_ROUTE_PATH };	
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
