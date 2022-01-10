using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Authorization
{
	/// <summary>
	/// Authorization constants
	/// </summary>
	public class Constants
	{
		/// <summary>
		/// Policy name for system administrators authorization  policy.
		/// </summary>
		/// 
		public const string SYSTEM_ADMIN_POLICY = "IsSystemAdmin";

		/// <summary>
		/// Policy name for site administrators authorization  policy.
		/// </summary>
		/// 
		public const string SITE_ADMIN_POLICY = "IsSiteAdmin";

		/// <summary>
		/// Policy name for page view permission authorization policy.
		/// </summary>
		/// 
		public const string PAGE_VIEW_POLICY = "HasPageViewPermission";

		/// <summary>
		///  Policy name for page edit permission authorization policy.
		/// </summary>
		/// 
		public const string PAGE_EDIT_POLICY = "HasPageEditPermission";

		/// <summary>
		///  Policy name for module edit permission authorization policy.
		/// </summary>
		/// 
		public const string MODULE_EDIT_POLICY = "HasModuleEditPermission";

		/// <summary>
		///  Policy name for site wizard permission authorization policy.
		/// </summary>
		/// 
		public const string SITE_WIZARD_POLICY = "SiteWizardAllowed";

		
	}
}
